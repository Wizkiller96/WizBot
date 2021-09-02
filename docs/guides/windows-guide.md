## Migration from 2.x 

⚠ If you're already hosting NadekoBot, You **MUST** update to latest version of 2.x and **run your bot at least once** before switching over to v3.

#### [Windows migration instructions](migration-instructions#windows)

## Setting Up NadekoBot on Windows With the Updater

| Table of Contents|
| :---------------------------------------------------------------------------------------------------------------------------|
| [Prerequisites](#prerequisites)                                                                                             |
| [Setup](#setup)                                                                                                             |
| [Starting the Bot](#starting-the-bot)                                                                                       |
| [Updating Nadeko](#updating-nadeko)                                                                                         |
| [Manually Installing the Prerequisites from the Updater](#if-the-updater-fails-to-install-the-prerequisites-for-any-reason) |

*Note: If you want to make changes to Nadeko's source code, please follow the [From Source][SourceGuide] guide instead.*

*If you have Windows 7 or a 32-bit system, please refer to the [From Source][SourceGuide] guide.*

#### Prerequisites

- Windows 8 or later (64-bit)
- [Create a Discord Bot application and invite the bot to your server](../../creds-guide.md)

**Optional**

- [Notepad++] (makes it easier to edit your credentials)
- [Visual C++ 2010 (x86)] and [Visual C++ 2017 (x64)] (both are required if you want Nadeko to play music - restart Windows after installation)

#### Setup

- Download and run the [NadekoBot v3 Updater][Updater].
- Click on the + at the top left to create a new bot.
 ![NadekoBot Updater](https://i.imgur.com/KZV49uf.png "NadekoBot Updater")
- Give your bot a name and then click **`Go to setup`** at the lower right.
 ![Create a new bot](https://i.imgur.com/Xnp7iQL.png "Create a new bot")
- Click on **`DOWNLOAD`** at the lower right
 ![Bot Setup](https://i.imgur.com/6RMXNqw.png "Bot Setup")
- Click on **`Install`** next to **`Redis`**.
- If you will use the music module, click on **`Install`** next to **`FFMPEG`** and **`Youtube-DL`**.
- If any dependencies fail to install, you can temporarily disable your Windows Defender/AV until you install them. If you don't want to, then read [the last section of this guide](#Manual-Prerequisite-Installation).
- When installation is finished, click on **`CREDS`** to the left of **`RUN`** at the lower right.
- Follow the guide on how to [Set up the credentials.json](../../jsons-explained) file.

#### Starting the bot

- Either click on **`RUN`** button in the updater or run the bot via its desktop shortcut.

#### Updating Nadeko

- Make sure Nadeko is closed and not running  
  (Run `.die` in a connected server to ensure it's not running).
- Open NadekoBot Updater
- Click on your bot at the upper left (looks like a spy).
- Click on **`Check for updates`**.
- If updates are available, you will be able to click on the Update button.
- Launch the bot
- You've updated and are running again, easy as that!

#### Manual Prerequisite Installation

You can still install them manually:

- [Redis Installer](https://github.com/MicrosoftArchive/redis/releases/tag/win-3.0.504) - Download and run the **`.msi`** file
- [ffmpeg-32bit] | [ffmpeg-64bit] - Download the **appropriate version** for your system (32 bit if you're running a 32 bit OS, or 64 if you're running a 64bit OS). Unzip it, and move `ffmpeg.exe` to a path that's in your PATH environment variable. If you don't know what that is, then just move the `ffmpeg.exe` file to NadekoBot/system
- [youtube-dl] - Click to download the file. Then put `youtube-dl.exe` in a path that's in your PATH environment variable. If you don't know what that is, then just move the `youtube-dl.exe` file to NadekoBot/system

### Windows From Source

##### Prerequisites

**Install these before proceeding or your bot will not work!**
- [.net 5](https://dotnet.microsoft.com/download/dotnet/5.0)  - needed to compile and run the bot
- [git](https://git-scm.com/downloads) - needed to clone the repository (you can also download the zip manually and extract it, but this guide assumes you're using git)
- [redis](https://github.com/MicrosoftArchive/redis/releases/download/win-3.0.504/Redis-x64-3.0.504.msi) - to cache things needed by some features and persist through restarts

##### Installation Instructions

Open PowerShell (press windows button on your keyboard and type powershell, it should show up; alternatively, right click the start menu and select Windows PowerShell), and navigate to the location where you want to install the bot (for example `cd ~/Desktop/`)  

1. `git clone https://gitlab.com/kwoth/nadekobot -b v3 --depth 1`
3. `dotnet publish -c Release -o output/ src/NadekoBot/`
4. `cd output && cp creds_example.yml creds.yml`
5. Open `creds.yml` with your favorite text editor (Please don't use Notepad or WordPad. You can use Notepad++, VSCode, Atom, Sublime, or something similar)
6. [Enter your bot's token](#creds-guide)
7. Run the bot `dotnet NadekoBot.dll` 
8. 🎉

##### Update Instructions

Open PowerShell as described above and run the following commands:

1. Navigate to your bot's folder, for example `cd ~/Desktop/nadekobot/src/NadekoBot`
2. Pull the latest updates (this will fail if you have custom code changes).
   - If you don't have custom code changes, just run `git pull`
   - If you do have custom code changes, You have 3 options
      - Undo all changes with `git checkout -- * && git pull`
      - Stash changes and try to re-apply them `git stash && git pull && git stash apply`
      - Commit your changes and resolve merge conflicts `git add . && git commit -m "My commit message" && git pull`
3. Re-run the bot `dotnet run -c Release`

⚠ You're expected to understand that your database will be in `bin/Release/<framework>/data/`, and if `<framework>` gets changed in the future, you will have to move your database manually.

#### Music prerequisites  
In order to use music commands, you need ffmpeg and youtube-dl installed.
- [ffmpeg-32bit] | [ffmpeg-64bit] - Download the **appropriate version** for your system (32 bit if you're running a 32 bit OS, or 64 if you're running a 64bit OS). Unzip it, and move `ffmpeg.exe` to a path that's in your PATH environment variable. If you don't know what that is, just move the `ffmpeg.exe` file to `NadekoBot/output`.
- [youtube-dl] - Click to download the file, then move `youtube-dl.exe` to a path that's in your PATH environment variable. If you don't know what that is, just move the `youtube-dl.exe` file to `NadekoBot/system`.

[Updater]: https://dl.nadeko.bot/v3
[Notepad++]: https://notepad-plus-plus.org/
[.net]: https://dotnet.microsoft.com/download/dotnet/5.0
[Redis]: https://github.com/MicrosoftArchive/redis/releases/download/win-3.0.504/Redis-x64-3.0.504.msi
[Visual C++ 2010 (x86)]: https://download.microsoft.com/download/1/6/5/165255E7-1014-4D0A-B094-B6A430A6BFFC/vcredist_x86.exe
[Visual C++ 2017 (x64)]: https://aka.ms/vs/15/release/vc_redist.x64.exe
[SourceGuide]: ../from-source
[ffmpeg-32bit]: https://cdn.nadeko.bot/dl/ffmpeg-32.zip
[ffmpeg-64bit]: https://cdn.nadeko.bot/dl/ffmpeg-64.zip
[youtube-dl]: https://yt-dl.org/downloads/latest/youtube-dl.exe

