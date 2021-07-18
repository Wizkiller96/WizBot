# :warning: Experimental Branch. Things will break. :warning:

## Migration from 2.x 

:warning: You **MUST** update to latest version of 2.x and **run your bot at least once** before switching over to v3 

## Changes

- Code cleanup
  - Command attributes cleaned up
    - Removed dummy Remarks and Usages attributes 
    - They were unused for a few patches but stayed in the code to avoid big git diffs
  - All database migrations and data (json file) migrations have been removed
    - As updating to the latest 2.x version before switching over to v3 is mandated (or fresh v3install), that means all migration code has ran and it can be safely removed 
  - There are 2 projects: NadekoBot and NadekoBot.Coordinator
    - You can directly run NadekoBot as the regular bot with one shard
    - Run NadekoBot.Coordinator if you want more control over your shards and a grpc api for coordinator with which you can start, restart, kill and see status of shards
- credentials.json moved to `creds.yml`
  - example is in `creds_example.yml`
  - most of the credentials from 2.x will be automatically migrated
- Guides
  - Now instruct users to set build output to nadekobot/output instead of running from nadekobot/src/NadekoBot/bin/release/net5
  - todo: Reworked from source installation (linux/windows) guide <todo link>
  - todo: Added docker support, installation guide at <todo link>