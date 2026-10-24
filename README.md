# Woordenspel

Een Wordfeud-kloon met Nederlandse woorden. Voorlopig alleen een HTTP-API, gebouwd in C# / .NET 10.

## Starten

Met Docker (de API draait dan op http://localhost:8080):

```bash
docker compose up -d --build
```

De database en de sleutel voor tokens staan in het volume `woordenspel-data` en blijven dus bewaard.
`docker compose down -v` gooit alles weg.

Zonder Docker:

```bash
dotnet run --project src/Woordenspel.Api   # data komt in ./App_Data
dotnet test
```

In Development staat het OpenAPI-document op `/openapi/v1.json`.
[`src/Woordenspel.Api/Woordenspel.Api.http`](src/Woordenspel.Api/Woordenspel.Api.http) bevat voorbeeldverzoeken.

## Opbouw

Clean Architecture: afhankelijkheden wijzen alleen naar binnen.

```
Api ──► Infrastructure ──► Application ──► Domain
```

| Project | Verantwoordelijkheid |
|---|---|
| `Woordenspel.Domain` | Alle spelregels, zonder afhankelijkheden. `Game` is het aggregate; `PlacementEvaluator` controleert zetten, vindt gevormde woorden en berekent de score. `User` bevat de regels voor gebruikersnamen en wachtwoorden. Toeval (`IRandomizer`) en het woordenboek (`IWordDictionary`) zijn interfaces. |
| `Woordenspel.Application` | Use cases in `GameService` en `AccountService`: laden, domein laten werken, opslaan, naar DTO's vertalen. Definieert de poorten `IGameRepository`, `IUserRepository` en `IPasswordHasher`. |
| `Woordenspel.Infrastructure` | SQLite via EF Core, de woordenlijst, wachtwoord-hashing (PBKDF2 van ASP.NET Core Identity) en `SystemRandomizer`. |
| `Woordenspel.Api` | Minimal API-endpoints, JWT-authenticatie en het vertalen van fouten naar `ProblemDetails`. |

### Opslag

Alles staat in één SQLite-bestand (`woordenspel.db` in de datamap, instelling `Storage:DataDirectory`).

- **Users**: gebruikersnaam (uniek, hoofdletterongevoelig) en wachtwoord-hash.
- **Games**: de volledige spelstand als JSON-snapshot (`Game.ToSnapshot()` / `Game.Restore()`), plus een
  versienummer. Proberen twee verzoeken tegelijk hetzelfde spel te wijzigen, dan krijgt het tweede een
  `409 ConcurrencyConflict` in plaats van dat het de eerste overschrijft.
- **GamePlayers**: welke gebruiker in welk spel zit, voor het snel opvragen van "mijn spellen".

Het schema wordt bij het opstarten bijgewerkt met EF Core-migraties. Na een wijziging aan het model maak je een
nieuwe migratie zo:

```bash
dotnet tool restore
dotnet ef migrations add <Naam> --project src/Woordenspel.Infrastructure \
  --startup-project src/Woordenspel.Api --output-dir Persistence/Migrations
```

Tests staan per laag in `tests/`. De Infrastructure- en API-tests gebruiken een echte SQLite-database
(in het geheugen of in een tijdelijke map). `Woordenspel.TestSupport` bevat gedeelde fakes: een randomizer die altijd 0 geeft
(de letterzak blijft op volgorde en de eerste speler begint) en een `FakeTimeProvider` om de klok vooruit te zetten.
Het domein leest zelf nooit de klok: acties krijgen het huidige tijdstip mee.

## Spelregels

- Bord van 15×15 met de standaard Wordfeud-indeling; het eerste woord gaat over het middelste vakje.
- 7 stenen per plankje, 2 spelers, 2 blanco stenen (0 punten). Wie begint, wordt willekeurig gekozen.
  Je kunt niet tegen jezelf spelen.
- Je hebt 72 uur per beurt (`turnDeadline` in de spelstand). Wie te laat is, verliest. Dit wordt verwerkt zodra
  het spel daarna wordt opgevraagd of iemand een actie doet.
- Alle stenen van een zet liggen in één rij of kolom, zonder gaten, en sluiten aan op wat er al ligt.
- Elk gevormd woord (ook kruiswoorden) moet in de woordenlijst staan.
- Premievakjes tellen alleen voor nieuw gelegde stenen. Alle 7 stenen leggen geeft 40 bonuspunten.
- Ruilen mag alleen als er nog minstens 7 stenen in de zak zitten.
- Het spel eindigt als iemand zijn plankje leegspeelt terwijl de zak leeg is, als elke speler twee keer achter elkaar
  past of ruilt, of als iemand opgeeft. Aan het eind gaan de punten op je plankje van je score af; wie zijn plankje
  leegspeelde, krijgt die punten van de tegenstander erbij.

Regels staan in `GameRules`, de letterverdeling in `LetterSet.Dutch` en de bordindeling in `BoardLayout.Standard`.

## API

### Account

Een gebruikersnaam heeft 3 tot 20 tekens (letters, cijfers of `_`), een wachtwoord minstens 8 tekens.
Registreren en inloggen geven een token terug (7 dagen geldig). Stuur dat mee als
`Authorization: Bearer <token>` bij alle `/api/games`-verzoeken.

| Methode | Route | |
|---|---|---|
| `POST` | `/api/account/register` | `{ "username": "anna", "password": "geheim123" }` → token |
| `POST` | `/api/account/login` | Zelfde body → token |
| `GET` | `/api/account/me` | Wie ben ik? |

Er wordt een willekeurige ondertekeningssleutel gemaakt en in de datamap bewaard. Wil je zelf een sleutel opgeven,
gebruik dan `Jwt:SigningKey` (minstens 32 tekens), bijvoorbeeld via de omgevingsvariabele `Jwt__SigningKey`.

### Spellen (inloggen vereist)

Alle acties gelden voor de ingelogde gebruiker.

| Methode | Route | |
|---|---|---|
| `POST` | `/api/games` | Nieuw spel tegen een andere gebruiker: `{ "opponent": "bram" }` |
| `GET` | `/api/games` | Mijn spellen; lopende spellen waarin ik aan de beurt ben eerst |
| `GET` | `/api/games/{gameId}` | Spelstand, inclusief mijn plankje |
| `POST` | `/api/games/{gameId}/preview` | Score van een zet berekenen zonder hem te spelen |
| `POST` | `/api/games/{gameId}/place` | Woord leggen: `{ "tiles": [{ "row": 7, "column": 7, "letter": "K", "isBlank": false }] }` |
| `POST` | `/api/games/{gameId}/swap` | Ruilen: `{ "letters": ["Q", "?"] }` (`?` is een blanco steen) |
| `POST` | `/api/games/{gameId}/pass` | Passen |
| `POST` | `/api/games/{gameId}/resign` | Opgeven |

### Overig (openbaar)

| Methode | Route | |
|---|---|---|
| `GET` | `/api/words/{word}` | Staat dit woord in de woordenlijst? |
| `GET` | `/api/setup` | Bordindeling, letterwaarden en regels |
| `GET` | `/health` | Draait de API? |

### Fouten

Fouten komen terug als `application/problem+json` met een stabiele `code`:

| Status | Wanneer | Voorbeelden van `code` |
|---|---|---|
| `401` | Geen of ongeldig token, of verkeerde inloggegevens | `AuthenticationFailed` |
| `404` | Spel of gebruiker bestaat niet, of je doet niet mee aan dit spel | `GameNotFound`, `UserNotFound`, `PlayerNotInGame` |
| `409` | Niet aan de beurt, spel voorbij, naam bezet, gelijktijdige wijziging | `NotYourTurn`, `GameFinished`, `UsernameTaken`, `ConcurrencyConflict` |
| `422` | Andere regelovertredingen | `InvalidWords` (met lijst `invalidWords`), `MustCoverCenter`, `InvalidPassword` |

## Woordenlijst

`data/woorden.txt` is gemaakt uit de [OpenTaal-woordenlijst](https://github.com/OpenTaal/opentaal-wordlist)
(Revised BSD / CC BY 3.0, zie `data/LICENSE-OpenTaal.txt`). Dat is ook de basis van de Nederlandse lijst van
Wordfeud. De officiële Scrabble-woordenlijst (OTC, op basis van Van Dale) is niet vrij beschikbaar.

[`scripts/build_woordenlijst.py`](scripts/build_woordenlijst.py) zet de lijst om volgens de gebruikelijke spelregels:

- Accenten weg: geëerd → GEEERD. "IJ" is gewoon een I plus een J.
- Apostroffen en streepjes weg, maar alleen als elk deel zelf een geldig woord is: taxi's → TAXIS en
  zee-egel → ZEEEGEL, maar tv-camera's valt af omdat TV een afkorting is.
- Geen eigennamen (hoofdletters), cijfers of spaties; 2 tot en met 15 letters.
- Woorden zonder klinker zijn afkortingen of symbolen (BTW, KG, NS) en vallen af, behalve de tussenwerpsels in
  [`data/tussenwerpsels.txt`](data/tussenwerpsels.txt) (HM, PST, ...).
- Afkortingen mét klinker (OV, SO, ...) staan in [`data/afkortingen.txt`](data/afkortingen.txt). Vul die aan als
  je er meer tegenkomt.

Opnieuw genereren (na het aanpassen van een van de lijsten, of voor een nieuwe versie van OpenTaal):

```bash
python3 scripts/build_woordenlijst.py
docker compose up -d --build   # de lijst zit in het image
```

Een ander bestand gebruiken, zoals een eigen exemplaar van de officiële lijst, kan via de instelling
`WordList:Path` (in Docker: `WordList__Path`): één woord per regel, in hoofdletters.
