#!/bin/sh

DATA=/app/data

# https://gitlab.com/WizNet/WizBot/commit/ddb892f96833670d3ac368a7f8eef42af4b810bf
[ -f "$DATA/pokemon/pokemon_abilities7.json" ] && [ ! -e "$DATA/pokemon/pokemon_abilities.json" ] && \
    mv "$DATA/pokemon/pokemon_abilities7.json" "$DATA/pokemon/pokemon_abilities.json"

[ -f "$DATA/pokemon/pokemon_list7.json" ] && [ ! -e "$DATA/pokemon/pokemon_list.json" ] && \
    mv "$DATA/pokemon/pokemon_list7.json" "$DATA/pokemon/pokemon_list.json"

[ -f "$DATA/pokemon/name-id_map4.json" ] && [ ! -e "$DATA/pokemon/name-id_map.json" ] && \
    mv "$DATA/pokemon/name-id_map4.json" "$DATA/pokemon/name-id_map.json"

# https://gitlab.com/WizNet/WizBot/commit/732dc9b6fb9efbddc31ae34cddbbbeac6dee21fb
[ -f "$DATA/hangman3.json" ] && [ ! -e "$DATA/hangman.json" ] && \
    mv "$DATA/hangman3.json" "$DATA/hangman.json"

rsync -rv --ignore-existing $DATA-default/ $DATA/

exec dotnet /app/WizBot.dll
