#!/bin/sh

set -e

data_init="/app/data_init"
data="/app/data"

cp -R -n $data_init/* $data
cp -n "$data_init/creds_example.yml" "$data/creds.yml"

yt-dlp -U || true

exec "$@"