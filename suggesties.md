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
7. ~~**Posities opslaan in de zetgeschiedenis.**~~ Gedaan: de laatste zet licht op in de frontend.
8. **Tegen de computer spelen.** De bot uit het testpotje omzetten naar C#, als tegenstander met een instelbaar
   niveau.
9. **Chat per spel** en **statistieken**: gewonnen en verloren spellen, gemiddelde score, beste woord, eventueel
   een rating.

## Techniek

10. ~~**Live updates met SignalR.**~~ Gedaan.
11. ~~**Een achtergrondtaak voor verlopen beurten.**~~ Gedaan: elke seconde wordt gecontroleerd of iemands klok op 0 staat.
12. **Een testpagina met inlogknop.** Met Scalar of Swagger UI kun je de API in de browser uitproberen, inclusief
    het meesturen van je token. Nu moet dat via het `.http`-bestand.
13. **CI met GitHub Actions**, die bij elke push de tests en de Docker-build draait.
14. **Betere foutmeldingen bij kapotte JSON.** Bij bijvoorbeeld `"letter": "AB"` komt nu een algemene 400-fout
    terug in plaats van een nette ProblemDetails-melding met uitleg.

## Een frontend

15. ~~**Een frontend.**~~ Gedaan: Blazor WebAssembly, met blitzspellen en een live klok.

## Blitz

16. **Tijdsinstelling kiezen** bij het starten: bijvoorbeeld 1, 3, 5 of 10 minuten. `GameRules.TimePerPlayer` staat
    al per spel opgeslagen; alleen het startverzoek en de lobby moeten de keuze doorgeven.
17. **Increment (Fischer):** een paar seconden extra na elke zet, zoals bij schaken. Dan word je niet gestraft voor
    een lang spel met veel korte zetten.
18. **Geluid of trilling** als je aan de beurt bent en in de laatste 30 seconden.
19. **Snel een tegenstander vinden:** een wachtrij voor blitzspellen, zodat je niet eerst een gebruikersnaam hoeft
    te weten.
