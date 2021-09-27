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

# fix folder permissions
chown -R nadeko:nadeko "$data"

# drop to regular user and launch command
exec sudo -u nadeko "$@"