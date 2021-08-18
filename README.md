## Migration from 2.x 

⚠ If you're already hosting NadekoBot, You **MUST** update to latest version of 2.x and **run your bot at least once** before switching over to v3 

#### Windows migration instructions

###### TODO

#### Linux migration instructions

1. In order to migrate your bot which is hosted on a **linux**, first update your current version to the latest 2.x version using the 2.x installer, run the bot, and make sure it works
   - Run the **old** installer `cd ~ && wget -N https://github.com/Kwoth/NadekoBot-BashScript/raw/1.9/linuxAIO.sh && bash linuxAIO.sh`
   - Run the 'download' option again
   - Run the bot
   - Type `.stats` and **make sure** the version is `2.46.5` or later
   - Stop the bot
2. Make sure your bot's folder is called `NadekoBot`
   - Run `cd ~ && ls`
   - Confirm there is a folder NadekoBot
3. Run the new installer, and run the options 1, 2 and 3 in that order to successfully migrate your bot's data  
   - Run the **new** installer `cd ~ && wget -N https://gitlab.com/Kwoth/nadeko-bash-installer/-/raw/master/linuxAIO.sh && bash linuxAIO.sh`
   - The installer should notify you that your data is ready for migration (message above the menu)
   - Install prerequisites (type `1` and press enter) and make sure it is successful
   - Download NadekoBot v3 (type `2` and press enter)
   - Run the bot (type `3` and press enter)
4. Make sure your permissions, custom reactions, credentials, and other data is preserved
   - You can try running `.stats` to ensure owner id is correct
   - `.lcr` to see custom reactions
   - `.lp` to list permissions
5. 🎉 Enjoy. If you want to learn how to update the bot, click (here)[#linux-updating-the-bot]

#### Manual migration 

⚠ NOT RECOMMENDED  
⚠ NadekoBot v3 requires .net 5

1. In order to migrate your bot which is hosted on a **linux**, first update your current version to the latest 2.x version using the 2.x installer, run the bot, and make sure it works
2. Rename your old nadeko bot folder to `nadekobot_2x`
   - `mv NadekoBot nadekobot_2x`
3. Build the new version and move old data to the output folder 
   1. Clone the v3 branch to a separate folder 
      - `git clone https://gitlab.com/kwoth/nadekobot -b v3 --depth 1`
   2. Build the bot
      - `dotnet publish -c Release -o output/ src/NadekoBot/`
   3. Copy old data
      - `cp -rf nadekobot_2x/src/NadekoBot/data nadekobot/src/NadekoBot/data`
   4. Copy the database 
      - `cp nadekobot_2x/src/NadekoBot/bin/Release/netcoreapp2.1/data/NadekoBot.db nadekobot/output/data`
   5. Copy credentials file
      - `cp nadekobot_2x/src/NadekoBot/credentials.json nadekobot/output/`
4. Run the bot
   - `cd nadekobot/output`
   - `dotnet NadekoBot.dll`
5. That's it. Just make sure that when you're updating the bot, you're properly backing up your old data

## Fresh Installation

- [Windows - Release](#windows-release)
- [Linux - Release](#linux-release)
- [Windows - From Source](#windows-from-source)
- [Linux - From Source](#linux-from-source)
- [Docker]

### Windows From Source

###### Prerequisites

Install these before proceeding
- [.net 5](https://dotnet.microsoft.com/download/dotnet/5.0)  - needed to compile and run the bot
- [git](https://git-scm.com/downloads) - needed to clone the repository (you can also download the zip manually and extract it but this guide assumes you're using git)
- [redis](https://github.com/MicrosoftArchive/redis/releases/download/win-3.0.504/Redis-x64-3.0.504.msi) - to cache things needed by some features and persist through restarts

###### Installation Instructions

Open PowerShell (press windows button on your keyboard and type powershell, it should show up), and navigate to the location where you want to install the bot (for example `cd ~/Desktop/`)  

1. `git clone https://gitlab.com/kwoth/nadekobot -b v3 --depth 1`
3. `dotnet publish -c Release -o output/ src/NadekoBot/`
4. `cd output && cp creds_example.yml creds.yml`
5. Open `creds.yml` with your favorite text editor (Please don't use notepad or wordpad. You can use notepad++, vscode, atom, sublime or something similar)
6. [Enter your bot's token](#creds-guide)
7. Run the bot `dotnet NadekoBot.dll` 
8. 🎉

###### Update Instructions (todo: WIP)

Open powershell and run following commands:

1. Navigate to your bot's folder, for example `cd ~/Desktop/nadekobot`
2. Pull the latest updates (this will fail if you have custom code changes).
   - If you don't have custom code changes, just run `git pull`
   - If you do have custom code changes (changes to .cs files) You have 3 options
      - Undo all changes with `git checkout -- * && git pull`
      - Stash changes and try to re-apply them `git stash && git pull && git stash apply`
      - Commit your changes and resolve merge conflicts `git add . && git commit -m "My commit message" && git pull`
3. Re-build the bot `dotnet publish -c Release -o output/ src/NadekoBot/`
4. Run the bot `cd output && dotnet NadekoBot.dll`

#### Music prerequisites  
In order to use music commands, you need ffmpeg and youtube-dl installed.
- [ffmpeg-32bit] | [ffmpeg-64bit] - Download the **appropriate version** for your system (32 bit if you're running a 32 bit OS, or 64 if you're running a 64bit OS). Unzip it, and move `ffmpeg.exe` to a path that's in your PATH environment variable. If you don't know what that is, then just move the `ffmpeg.exe` file to nadekobot/output
- [youtube-dl] - Click to download the file. Then put `youtube-dl.exe` in a path that's in your PATH environment variable. If you don't know what that is, then just move the `youtube-dl.exe` file to NadekoBot/system

### Linux From Source

Open Terminal (if you're on a linux with window manager) and navigate to the location where you want to install the bot (for example `cd ~`) 

###### Installation Instructions

1. Download and run the **new** installer script `cd ~ && wget -N https://gitlab.com/Kwoth/nadeko-bash-installer/-/raw/master/linuxAIO.sh && bash linuxAIO.sh`
2. Install prerequisites (type `1` and press enter)
3. Download the bot (type `2` and press enter)
4. Exit the installer in order to set up your `creds.yml` 
5. Copy the creds.yml template `cp nadekobot/output/creds_example.yml nadekobot/output/creds.yml` 
6. Open `nadekobot/output/creds.yml` with your favorite text editor. We will use nano here
7. `nano nadekobot/output/creds.yml`
8. [Enter your bot's token](#creds-guide)
9. Run the bot (type `3` and press enter)

###### Update Instructions

1. ⚠ Stop the bot
2. Download and run the **new** installer script `cd ~ && wget -N https://gitlab.com/Kwoth/nadeko-bash-installer/-/raw/master/linuxAIO.sh && bash linuxAIO.sh`
3. Update the bot (type `2` and press enter)
4. Run the bot (type `3` and press enter)
5. 🎉 

## Creds Guide

This document aims to guide you through the process of creating a Discord account for your bot 
(the Discord Bot application), and inviting that account into your Discord server.

![Create a bot application and copy token to creds.yml file](https://cdn.nadeko.bot/tutorial/bot-creds-guide.gif)

- Go to [the Discord developer application page][DiscordApp].
- Log in with your Discord account.
- Click **New Application**
- Fill out the `Name` field (it's your app's name)
- Go to the **Bot** tab on the left sidebar.
- Click on the `Add a Bot` button and confirm that you do want to add a bot to this app.
- **Optional:** Add bot's avatar and description
- Copy Token to `creds.yml`
- Scroll down to the `Privileged Gateway Intents` section and enable both intents.
  These are required for a number of features to function properly, and should both be on.

#### Inviting your bot to your server    

![Invite the bot to your server](https://cdn.nadeko.bot/tutorial/bot-invite-guide.gif)

- On the **General Information** tab, copy your `Application ID` from your [applications page][DiscordApp].
- Replace the `YOUR_CLIENT_ID_HERE` in this link:
  `https://discordapp.com/oauth2/authorize?client_id=YOUR_CLIENT_ID_HERE&scope=bot&permissions=66186303` with your `Client ID`
- The link should now look something like this:
  `https://discordapp.com/oauth2/authorize?client_id=123123123123&scope=bot&permissions=66186303`
- Access that newly created link, pick your Discord server, click `Authorize` and confirm with the captcha at the end
- The bot should now be in your server

That's it! You may now go back to the installation guide you were following previously 🎉

[DiscordApp]: https://discordapp.com/developers/applications/me
[ffmpeg-32bit]: https://cdn.nadeko.bot/dl/ffmpeg-32.zip
[ffmpeg-64bit]: https://cdn.nadeko.bot/dl/ffmpeg-64.zip
[youtube-dl]: https://yt-dl.org/downloads/latest/youtube-dl.exe
[docs]: https://nadekobot.rtfd.io