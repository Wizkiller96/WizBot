# Changelog

Experimental changelog. Mostly based on [keepachangelog](https://keepachangelog.com/en/1.0.0/) except date format. a-c-f-r-o

## [2.46.0] - 17.06.2021

### Added

- Added some nsfw commands

### Changed

- `.aar` reworked. Now supports multiple roles, up to 3.
  - Toggle roles that are added to newly joined users with `.aar RoleName`
  - Use `.aar` to list roles which will be added
  - Roles which are deleted are automatically cleaned up from `.aar`
- `.inrole` now also shows user ids
- Blacklist commands (owner only) `.ubl` `.sbl` and `.cbl` will now list blacklisted items when no argument (or a page number) is provided
- `.cmdcd` now works with customreactions too
- `.xprr` usage changed. It now takes add/rm parameter to add/remove a role ex. You can only take or remove a single role, adding and removing a role at the same level doesn't work (yet?)
    - example: `.xprr 5 add Member` or `.xprr 1 rm Newbie`

## [2.45.2] - 14.06.2021

### Added

- Added `.duckduckgo / .ddg` search

### Changed

- `.invlist` shows expire time and is slightly prettier

### Fixed

- `.antialt` will be properly cleaned up when the bot leaves the server

## [2.45.1] - 12.06.2021

### Added

- Added many new aliases to custom reaction commands in the format ex + "action" to prepare for the future rename from CustomReactions to Expressions
- You can now `.divorce` via username#discrim even if the user no longer exists

### Changed

- DmHelpText should now have %prefix% and %bot.prefix% placeholders available
- Added squares which show enabled features for each cr in `.lcr`
- Changed CustomReactions' IDs to show, and accept base 32 unambigous characters instead of the normal database IDs (this will result in much shorter cr IDs in case you have a lot of them)
- Improved `.lcr` helptext to explain what's shown in the output
- `.rolecolor <color> <role>` changed to take color, then the role, to make it easier to set color for roles with multiple words without mentioning the role
- `.acmdcds` alias chanaged to `.cmdcds`
- `.8ball` will now cache results for a day
- `.chatmute` and `.voicemute` now support timed mutes

### Fixed

- Fixed `.config <conf> <prop>` exceeding embed field character limit

## [2.45.0] - 10.06.2021

### Added

- Added `.crsexport` and `.crsimport` 
  - Allows for quick export/import of server or global custom reactions
  - Requires admin permissions for server crs, and owner for global crs
  - Explanation of the fields is in the comment at the top of the `.crsexport` .yml file
- Added `.mquality` / `.musicquality` - Set encoding quality. Has 4 presets - Low, Medium, High, Highest. Default is Highest
- Added `.xprewsreset` which resets all currently set xp level up rewards
- Added `.purgeuser @User` which will remove the specified from the database completely. Removed settings include: Xp, clubs, waifu, currency, etc...
- Added `.config xp txt.per_image` and xpFromImage to xp.yml - Change this config to allow xp gain from posting images. Images must be 128x128 or greater in size
- Added `.take <amount> <role>` to complement `.award <amount> role`
- Added **Fans** list to `.waifuinfo` which shows how many people have their affinity set to you
- Added `.antialt` which will punish any user whose account is younger than specified threshold

### Changed

- `.warne` with no args will now show current state
- .inrole` will now lists users with no roles if no role is provided
- Music suttering fixed on some systems
- `.say` moved to utility module
- Re-created GuildRepeaters table and renamed to Repeaters
- confirmation prompts will now use pending color from bot config, instead of okcolor
- `.mute` can now have up to 49 days mute to match .warnp
- `.warnlog` now has proper pagination (with reactions) and checking your own warnings past page 1 works correctly now with `.warnlog 2`

### Fixed

- obsolete_use string fixed
- Fixed `.crreact`

## [2.44.4] - 06.06.2021

### Added

- Re-added `%music.playing%` and `%music.queued%` (#290)
- Added `%music.servers%` which shows how many servers have a song queued up to play  
ℹ️ ^ Only available to `.ropl` / `.adpl` feature atm
- `.autodc` re-added
- `.qrp`, `.vol`, `.smch` `.autodc` will now persist

### Changed

- Using `.commands` / `.cmds` without a module will now list modules
- `.qrp` / `.queuerepeat` will now accept one of 3 values
    - `none` - don't repeat queue
    - `track` - repeat single track
    - `queue` (or ommit) - repeat entire queue
- your old `.defvol` and `.smch` settings will be reset

### Fixed

- Fixed `.google` / `.g` command
- Removing last song in the queue will no longer reset queue index
- Having `.rpl` disabled will now correctly stop after the last song, closes #292

### Removed 

- `.sad` removed. It's more or less useless. Use `.qrp` and `.autodc` now for similar effect

### Obsolete

- `.rcs` is obsolete, use `.qrp s` or `.qrp song`
- `.defvol` is obsolete, use `.vol`

## [2.44.3] - 04.06.2021

### Changed

- Minor perf improvement for filter checks

### Fixed 

- `.qs` result urls are now valid
- Custom reactions with "`-`" as a response should once again disable that custom reaction completely
- Fixed `.acrm` out of range string
- Fixed `.sclist` and `.aclist` not showing correct indexes past page 1

## [2.44.2] - 02.06.2021

### Added

- Music related commands reimplemented with custom code, **considered alpha state**
- Song and playlist caching (faster song queue after first time)
- Much faster starting and skipping once the songs are in the queue
- Higher quality audio (no stuttering too!)
- Local tracks will now have durations if you have ffprobe installed (comes with ffmpeg)
- Bot supports joining a different vc without skipping the song if you use `.j` 
  - ⚠️ **DO NOT DRAG THE BOT** to another vc, as it's not properly supported atm, and you will have to do `.play` after dragging it) 
- `.j` makes the bot join your voice channel
- `.p` is now alias of play, pause is `.pause`
- `.qs` should work without google api key now for most users as it is using a custom loader
- Added `.clubs` alias for `.clublb`

### Changed

- `.ms` no longer takes `>` between arguments (`.ms 1 5` now, was `.ms 1>5` before)
- FlowerShop renamed to Shop

### Fixed

- Fixed decay bug giving everyone 1 flower every 24h
- Fixed feeds which have rss media items without a type
- Fixed `.acrm` index not working
- Fixed and error reply when a waifu item doesn't exist
- Disabled colored console on windows as they were causing issues for some users
- Fixed/Updated some strings and several minor bugfixes

### Removed

- Removed admin requirement on `.scrm` as it didn't make sense
- Some Music commands are removed because of the complexity they bring in with little value (if you *really* want them back, you can open an issue and specify your *good* reason)