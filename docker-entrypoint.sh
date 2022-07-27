#!/bin/sh
set -e;

data_init=/app/data_init
data=/app/data

# populate /app/data if empty
for i in $(ls $data_init)
do
    if [ ! -e "$data/$i" ]; then
        [ -f "$data_init/$i" ] && cp "$data_init/$i" "$data/$i"
        [ -d "$data_init/$i" ] && cp -r "$data_init/$i" "$data/$i"
    fi
done

# creds.yml migration
if [ -f /app/creds.yml ]; then
    echo "Default location for creds.yml is now /app/data/creds.yml."
    echo "Please move your creds.yml and update your docker-compose.yml accordingly."

    export Nadeko_creds=/app/creds.yml
fi

# ensure nadeko can write on /app/data
chown -R nadeko:nadeko "$data"

# drop to regular user and launch command
exec sudo -u nadeko "$@"