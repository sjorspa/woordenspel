#!/usr/bin/env python3
"""Bouwt data/woorden.txt uit de OpenTaal-woordenlijst, met regels zoals Wordfeud en Scrabble ze hanteren.

Gebruik:
    python3 scripts/build_woordenlijst.py                 # downloadt de lijst van OpenTaal
    python3 scripts/build_woordenlijst.py wordlist.txt    # gebruikt een lokaal bestand

Regels:
- accenten weg: geëerd -> GEEERD;
- apostroffen en streepjes weg, maar alleen als de delen zelf geldige woorden zijn:
  taxi's -> TAXIS en zee-egel -> ZEEEGEL, maar zm'ers en tv-journalen vallen af (ZM en TV zijn afkortingen);
- geen hoofdletters (eigennamen), cijfers, spaties of andere tekens; 2 tot en met 15 letters;
- woorden zonder klinker zijn afkortingen of symbolen (BTW, KG), behalve die in data/tussenwerpsels.txt;
- woorden in data/afkortingen.txt vallen ook af.
"""
import re
import sys
import unicodedata
import urllib.request
from pathlib import Path

SOURCE_URL = "https://raw.githubusercontent.com/OpenTaal/opentaal-wordlist/master/wordlist.txt"
DATA = Path(__file__).resolve().parent.parent / "data"
LETTERS_ONLY = re.compile(r"^[a-z]{2,15}$")
HAS_VOWEL = re.compile(r"[AEIOUY]")


def read_list(path):
    lines = (line.strip() for line in path.read_text(encoding="utf-8").splitlines())
    return {line.upper() for line in lines if line and not line.startswith("#")}


def normalize(word):
    """Geeft het speelbare woord in hoofdletters, of None als het geen speelbaar woord is."""
    without_accents = "".join(
        c for c in unicodedata.normalize("NFD", word) if unicodedata.category(c) != "Mn")
    return without_accents.upper() if LETTERS_ONLY.match(without_accents) else None


def is_playable(word, interjections, abbreviations):
    return word not in abbreviations and (HAS_VOWEL.search(word) is not None or word in interjections)


def main():
    if len(sys.argv) > 1:
        source = Path(sys.argv[1]).read_text(encoding="utf-8")
    else:
        with urllib.request.urlopen(SOURCE_URL) as response:
            source = response.read().decode("utf-8")

    interjections = read_list(DATA / "tussenwerpsels.txt")
    abbreviations = read_list(DATA / "afkortingen.txt")

    words, compounds = set(), []
    for line in source.splitlines():
        line = line.strip()
        if "-" in line or "'" in line:
            compounds.append(line)
        elif (word := normalize(line)) and is_playable(word, interjections, abbreviations):
            words.add(word)

    # Pas na de gewone woorden, zodat we weten welke delen geldig zijn.
    for line in compounds:
        stems = [normalize(part.split("'")[0]) for part in line.split("-")]
        word = normalize(line.replace("-", "").replace("'", ""))
        if word and all(stem in words for stem in stems):
            words.add(word)

    output = DATA / "woorden.txt"
    output.write_text("".join(f"{w}\n" for w in sorted(words)), encoding="utf-8")
    print(f"{len(words)} woorden geschreven naar {output}")


if __name__ == "__main__":
    main()
