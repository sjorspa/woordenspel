# Suggesties voor Woordenspel

Mijn advies: begin met 1, 2 en 3 (klein, en ze voorkomen fouten of misbruik). Daarna 7, omdat een frontend die
posities nodig heeft. Dan heb je een goede basis voor 15: het spel echt in de browser spelen.

## Nog open uit eerdere stappen

1. **De letterverdeling controleren.** `LetterSet.Dutch` telt nu 103 stenen. Het echte Wordfeud heeft er volgens
   mij 104, dus waarschijnlijk klopt er één aantal niet.
2. **De regel voor passen kiezen.** Nu eindigt het spel pas als elke speler 2 keer op rij heeft gepast of geruild
   (4 beurten zonder punten bij 2 spelers). Moet dat 2 keer in totaal zijn? Dat is `ScorelessTurnsPerPlayerToEnd`
   in `GameRules`.

## Voordat de API online gaat

3. **Rate limiting op inloggen en registreren.** Nu kan iemand onbeperkt wachtwoorden proberen. ASP.NET Core
   heeft dit ingebouwd.
4. **HTTPS via een reverse proxy**, bijvoorbeeld Caddy of nginx in `docker-compose.yml`.
5. **CORS**, zodra er een webfrontend op een ander domein komt.

## Spelfuncties zoals in Wordfeud

6. **Uitnodigingen.** Nu verschijnt een spel meteen bij je tegenstander. Met uitnodigingen kan die eerst
   accepteren of weigeren. Daarnaast: een willekeurige tegenstander zoeken, of meteen een revanche starten.
7. **Posities opslaan in de zetgeschiedenis.** `MoveRecord` bewaart nu alleen de woorden en de score, niet waar
   de stenen liggen. Een app kan de laatste zet daardoor niet oplichten of een spel terugspelen.
8. **Tegen de computer spelen.** De bot uit het testpotje omzetten naar C#, als tegenstander met een instelbaar
   niveau.
9. **Chat per spel** en **statistieken**: gewonnen en verloren spellen, gemiddelde score, beste woord, eventueel
   een rating.

## Techniek

10. **Live updates met SignalR.** Dan ziet je tegenstander je zet meteen, zonder steeds te hoeven verversen.
11. **Een achtergrondtaak voor verlopen beurten.** Een verlopen beurt wordt nu pas verwerkt als iemand het spel
    opent. Dit is nodig voor meldingen als "je tegenstander was te laat".
12. **Een testpagina met inlogknop.** Met Scalar of Swagger UI kun je de API in de browser uitproberen, inclusief
    het meesturen van je token. Nu moet dat via het `.http`-bestand.
13. **CI met GitHub Actions**, die bij elke push de tests en de Docker-build draait.
14. **Betere foutmeldingen bij kapotte JSON.** Bij bijvoorbeeld `"letter": "AB"` komt nu een algemene 400-fout
    terug in plaats van een nette ProblemDetails-melding met uitleg.

## Een frontend

15. Nu is er alleen een API. Een simpele webclient met een bord, een plankje en slepen van stenen maakt het echt
    speelbaar. Kies je ooit voor een app, dan is dit ook een handige basis.
