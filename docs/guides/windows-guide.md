## Setting Up WizBot on Windows With the Updater

| Table of Contents                                                              |
|:-------------------------------------------------------------------------------|
| [Prerequisites](#prerequisites)                                                |
| [Setup](#setup)                                                                |
| [Starting the Bot](#starting-the-bot)                                          |
| [Updating WizBot](#updating-wizbot)                                            |
| [Manually Installing the Prerequisites from the Updater](#music-prerequisites) |

*Note: If you want to make changes to Wiz's source code, please follow the [From Source](#windows-from-source) guide instead.*

*If you have Windows 7 or a 32-bit system, please refer to the [From Source](#windows-from-source)) guide.*

#### Prerequisites

- Windows 8 or later (64-bit)
- [Create a Discord Bot application and invite the bot to your server](../creds-guide.md)

**Optional**

- [Notepad++] (makes it easier to edit your credentials)
- [Visual C++ 2010 (x86)] and [Visual C++ 2017 (x64)] (both are required if you want WizBot to play music - restart Windows after installation)

#### Setup

- Download and run the [WizBot v3 Updater][Updater].
- Click on the + at the top left to create a new bot.
- Give your bot a name and then click **`Go to setup`** at the lower right.
- Click on **`DOWNLOAD`** at the lower right
- Click on **`Install`** next to **`Redis`**.
- **(Note: Redis is optional unless you are are using the bot on 2000+ servers)**
- Note: If Redis fails to install, install Redis manually here: [Redis Installer](https://github.com/MicrosoftArchive/redis/releases/tag/win-3.0.504) Download and run the **`.msi`** file.
- If you will use the music module, click on **`Install`** next to **`FFMPEG`** and **`Youtube-DL`**.
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

- [Redis Installer](https://github.com/MicrosoftArchive/redis/releases/tag/win-3.0.504) - Download and run the **`.msi`** file
- [ffmpeg-32bit] | [ffmpeg-64bit] - Download the **appropriate version** for your system (32 bit if you're running a 32 bit OS, or 64 if you're running a 64bit OS). Unzip it, and move `ffmpeg.exe` to a path that's in your PATH environment variable. If you don't know what that is, then just move the `ffmpeg.exe` file to WizBot/system
- [youtube-dl] - Click to download the file. Then put `youtube-dl.exe` in a path that's in your PATH environment variable. If you don't know what that is, then just move the `youtube-dl.exe` file to WizBot/system

## **⚠ IF YOU ARE FOLLOWING THE GUIDE ABOVE, IGNORE THIS SECTION ⚠**

### Windows From Source

##### Prerequisites

**Install these before proceeding or your bot will not work!**
- [.net 6](https://dotnet.microsoft.com/download/dotnet/6.0)  - needed to compile and run the bot
- [git](https://git-scm.com/downloads) - needed to clone the repository (you can also download the zip manually and extract it, but this guide assumes you're using git)
- [redis](https://github.com/MicrosoftArchive/redis/releases/download/win-3.0.504/Redis-x64-3.0.504.msi) - to cache things needed by some features and persist through restarts

##### Installation Instructions

Open PowerShell (press windows button on your keyboard and type powershell, it should show up; alternatively, right click the start menu and select Windows PowerShell), and navigate to the location where you want to install the bot (for example `cd ~/Desktop/`)  

1. `git clone https://gitlab.com/WizNet/WizBot -b v4 --depth 1`
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
3. Pull the new version
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
In order to use music commands, you need ffmpeg and youtube-dl installed.
- [ffmpeg-32bit] | [ffmpeg-64bit] - Download the **appropriate version** for your system (32 bit if you're running a 32 bit OS, or 64 if you're running a 64bit OS). Unzip it, and move `ffmpeg.exe` to a path that's in your PATH environment variable. If you don't know what that is, just move the `ffmpeg.exe` file to `WizBot/output`.
- [youtube-dl] - Click to download the file, then move `youtube-dl.exe` to a path that's in your PATH environment variable. If you don't know what that is, just move the `youtube-dl.exe` file to `WizBot/system`.

[Updater]: https://dl.wizbot.cc/
[Notepad++]: https://notepad-plus-plus.org/
[.net]: https://dotnet.microsoft.com/download/dotnet/5.0
[Redis]: https://github.com/MicrosoftArchive/redis/releases/download/win-3.0.504/Redis-x64-3.0.504.msi
[Visual C++ 2010 (x86)]: https://download.microsoft.com/download/1/6/5/165255E7-1014-4D0A-B094-B6A430A6BFFC/vcredist_x86.exe
[Visual C++ 2017 (x64)]: https://aka.ms/vs/15/release/vc_redist.x64.exe
[ffmpeg-32bit]: https://cdn.wizbot.cc/dl/ffmpeg-32.zip
[ffmpeg-64bit]: https://cdn.wizbot.cc/dl/ffmpeg-64.zip
[youtube-dl]: https://yt-dl.org/downloads/latest/youtube-dl.exe
