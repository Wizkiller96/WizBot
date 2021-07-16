# :warning: Experimental Branch. Things will break. :warning:

## Migration from 2.x 

:warning: You **MUST** update to latest version of 2.x and **run yourbot at least once** before switching over to v3  

## Changes

- explain properly: Command attributes cleaned up
- explain properly: Database migrations cleaned up/squashed
- explain properly: coord and coord.yml
- wip: credentials.json moved to `creds.yml`, example is in `creds_example.yml`
- todo: from source run location is nadekobot/output
- todo: votes functionality changed
- todo: code cleanup tasks
    - todo: remove colors from bot.cs
    - todo: creds
- todo: from source installation (linux/windows) guide
- todo: docker installation guide
- todo?: maybe use https://github.com/Humanizr/Humanizer for trimto, time printing, date printing, etc
- todo?: use guild locale more in the code (from guild settings) (for dates, currency, etc?)
- todo?: Write a sourcegen for response strings and use const/static fields (maybe even typed to enforce correct number of arguments)