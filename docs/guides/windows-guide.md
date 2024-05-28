## Setting Up WizBot on Windows With the Updater

| Table of Contents                                                              |
|:-------------------------------------------------------------------------------|
| [Prerequisites](#prerequisites)                                                |
| [Setup](#setup)                                                                |
| [Starting the Bot](#starting-the-bot)                                          |
| [Updating WizBot](#updating-wizbot)                                            |
| [Manually Installing the Prerequisites from the Updater](#music-prerequisites) |

*Note: If you want to make changes to WizBot's source code, please follow the [From Source](#windows-from-source) guide instead.*

#### Prerequisites

- Windows 10 or later (64-bit)
- [Create a Discord Bot application and invite the bot to your server](../creds-guide.md)

**Optional**

- [Visual Studio Code](https://code.visualstudio.com/Download) (Highly suggested if you plan on editing files)
- [Visual C++ 2010 (x86)] and [Visual C++ 2017 (x64)] (both are required if you want WizBot to play music - restart Windows after installation)

#### Setup
!!! Warning 
    Updater not available.

- Download and run the [WizBot v3 Updater][Updater].
- Click on the + at the top left to create a new bot.
<!-- ![WizBot Updater](https://i.imgur.com/FmR7F7o.png "WizBot Updater") -->
- Give your bot a name and then click **`Go to setup`** at the lower right.
<!-- ![Create a new bot](https://i.imgur.com/JxtRk9e.png "Create a new bot") -->
- Click on **`DOWNLOAD`** at the lower right
<!-- ![Bot Setup](https://i.imgur.com/HqAl36p.png "Bot Setup") -->
- **Note: Redis is optional. install Redis manually here: [Redis] Download and run the **`.msi`** file.**
- If you will use the music module, click on **`Install`** next to **`FFMPEG`** and **`Youtube-DLP`**.
- If any dependencies fail to install, you can temporarily disable your Windows Defender/AV until you install them. If you don't want to, then read [the last section of this guide](#Manual-Prerequisite-Installation).
- When installation is finished, click on **`CREDS`** to the left of **`RUN`** at the lower right.
- Follow the guide on how to [Set up the creds.yml](../../creds-guide) file.

#### Starting the bot

- Either click on **`RUN`** button in the updater or run the bot via its desktop shortcut.

### If you get a "No owner channels created..." message. Please follow the creds guide again [**HERE**](../../creds-guide).

#### Updating WizBot

- Make sure WizBot is closed and not running
  (Run `.die` in a connected server to ensure it's not running).
- Open WizBot Updater
- Click on your bot at the upper left (looks like a spy).
- Click on **`Check for updates`**.
- If updates are available, you will be able to click on the Update button.
- Launch the bot
- You've updated and are running again, easy as that!

#### Manual Prerequisite Installation

You can still install them manually:

- [Redis] (OPTIONAL) - Download and run the **`.msi`** file
- [ffmpeg-32bit] | [ffmpeg-64bit] - Download the **appropriate version** for your system (32 bit if you're running a 32 bit OS, or 64 if you're running a 64bit OS). Unzip it, and move `ffmpeg.exe` to a path that's in your PATH environment variable. If you don't know what that is, then just move the `ffmpeg.exe` file to WizBot/system
- [youtube-dlp] - Click to download the `yt-dlp.exe` file then put `yt-dlp.exe` in a path that's in your PATH environment variable. If you don't know what that is, then just move the `yt-dlp.exe` file to WizBot/system

## **⚠ IF YOU ARE FOLLOWING THE GUIDE ABOVE, IGNORE THIS SECTION ⚠**

### Windows From Source

##### Prerequisites

**Install these before proceeding or your bot will not work!**
- [.net 8](https://dotnet.microsoft.com/en-us/download)  - needed to compile and run the bot
- [git](https://git-scm.com/downloads) - needed to clone the repository (you can also download the zip manually and extract it, but this guide assumes you're using git)
- [Redis] (OPTIONAL)- to cache things needed by some features and persist through restarts

##### Installation Instructions

Open PowerShell (press windows button on your keyboard and type powershell, it should show up; alternatively, right click the start menu and select Windows PowerShell), and navigate to the location where you want to install the bot (for example `cd ~/Desktop/`)

1. `git clone https://gitlab.com/WizNet/WizBot -b v5 --depth 1`
2. `cd wizbot`
3. `dotnet publish -c Release -o output/ src/WizBot/`
4. `cd output`
5. `cp creds_example.yml creds.yml`
6. Open `creds.yml` with your favorite text editor (Please don't use Notepad or WordPad. You can use Notepad++, VSCode, Atom, Sublime, or something similar)
7. [Enter your bot's token](#creds-guide)
8. Run the bot `dotnet WizBot.dll`
9. 🎉

##### Update Instructions

Open PowerShell as described above and run the following commands:

1. Stop the bot
  - ⚠️ Make sure you don't have your database, credentials or any other wizbot folder open in some application, this might prevent some of the steps from executing succesfully
2. Navigate to your bot's folder, example:
    - `cd ~/Desktop/wizbot`
3. Pull the new version, and make sure you're on the v5 branch
    - *⚠️ the first 3 lines can be omitted if you're already on v5. If you're updating from v4, you must run them*
        - `git remote set-branches origin '*'`
        - `git fetch -v --depth=1`
        - `git checkout v5`
    - `git pull` 
    - ⚠️ If this fails, you may want to stash or remove your code changes if you don't know how to resolve merge conflicts
4. **Backup** old output in case your data is overwritten
    - `cp -r -fo output/ output-old`
5. Build the bot again
    - `dotnet publish -c Release -o output/ src/WizBot/`
6. Remove old strings and aliases to avoid overwriting the updated versions of those files
    - ⚠ If you've modified said files, back them up instead
    - `rm output-old/data/aliases.yml`
    - `rm -r output-old/data/strings`
7. Copy old data
    - `cp -Recurse .\output-old\data\ .\output\ -Force`
8. Copy creds.yml
    - `cp output-old/creds.yml output/`
9. Run the bot
    - `cd output`
    - `dotnet WizBot.dll`

🎉 Enjoy

#### Music prerequisites
In order to use music commands, you need ffmpeg and yt-dlp installed.
- [ffmpeg-32bit] | [ffmpeg-64bit] - Download the **appropriate version** for your system (32 bit if you're running a 32 bit OS, or 64 if you're running a 64bit OS). Unzip it, and move `ffmpeg.exe` to a path that's in your PATH environment variable. If you don't know what that is, just move the `ffmpeg.exe` file to `WizBot/output`.
- [youtube-dlp] - Click to download the `yt-dlp.exe` file, then move `yt-dlp.exe` to a path that's in your PATH environment variable. If you don't know what that is, just move the `yt-dlp.exe` file to `WizBot/system`.

[Updater]: https://dl.wizbot.cc/v3/
[Notepad++]: https://notepad-plus-plus.org/
[.net]: https://dotnet.microsoft.com/download/dotnet/5.0
[Redis]: https://github.com/MicrosoftArchive/redis/releases/download/win-3.0.504/Redis-x64-3.0.504.msi
[Visual C++ 2010 (x86)]: https://download.microsoft.com/download/1/6/5/165255E7-1014-4D0A-B094-B6A430A6BFFC/vcredist_x86.exe
[Visual C++ 2017 (x64)]: https://aka.ms/vs/15/release/vc_redist.x64.exe
[ffmpeg-32bit]: https://cdn.wizbot.cc/dl/ffmpeg-32.zip
[ffmpeg-64bit]: https://cdn.wizbot.cc/dl/ffmpeg-64.zip
[youtube-dlp]: https://github.com/yt-dlp/yt-dlp/releases
