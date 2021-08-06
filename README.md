## Migration from 2.x 

⚠ If you're already hosting NadekoBot, You **MUST** update to latest version of 2.x and **run your bot at least once** before switching over to v3 

todo: how to migrate 2.x repo to v3 repo

## Installation

- [Windows - Release](#windows-release)
- [Linux - Release](#linux-release)
- [Windows - From Source ](#windows-from-source)
- [Linux - From Source](#linux-from-source)
- [Docker]

### Windows From Source

###### Prerequisites

Install these before proceeding
- [.net 5](https://dotnet.microsoft.com/download/dotnet/5.0)  - needed to compile and run the bot
- [git](https://git-scm.com/downloads) - needed to clone the repository (you can also download the zip manually and extract it but this guide assumes you're using git)
- [redis](https://github.com/MicrosoftArchive/redis/releases/download/win-3.0.504/Redis-x64-3.0.504.msi) - to cache things needed by some features and persist through restarts

###### Instructions

Open PowerShell (press windows button on your keyboard and type powershell, it should show up), and navigate to the location where you want to install the bot (for example `cd ~/Desktop/`)  

1. `git clone https://gitlab.com/kwoth/nadekobot -b v3 --depth 1`
3. `dotnet publish -c Release -o output/ src/NadekBot/`
4. `cd output && cp creds_example.yml creds.yml`
5. Open `creds.yml` with your favorite text editor (Please don't use notepad or wordpad. You can use notepad++, vscode, atom, sublime or something similar)
6. [Enter your bot's token](#creds-guide)
7. Run the bot `dotnet NadekoBot.dll` 
8. 🎉


#### Music prerequisites  
In order to use music commands, you need ffmpeg and youtube-dl installed.
- [ffmpeg-32bit] | [ffmpeg-64bit] - Download the **appropriate version** for your system (32 bit if you're running a 32 bit OS, or 64 if you're running a 64bit OS). Unzip it, and move `ffmpeg.exe` to a path that's in your PATH environment variable. If you don't know what that is, then just move the `ffmpeg.exe` file to nadekobot/output
- [youtube-dl] - Click to download the file. Then put `youtube-dl.exe` in a path that's in your PATH environment variable. If you don't know what that is, then just move the `youtube-dl.exe` file to NadekoBot/system

### Linux From Source

Open Terminal (if you're on a linux with window manager) and navigate to the location where you want to install the bot (for example `cd ~`)

###### Prerequisites

- [.net 5](https://dotnet.microsoft.com/download/dotnet/5.0)
- [git](https://git-scm.com/downloads)

###### Instructions

1. `git clone https://gitlab.com/kwoth/nadekobot -b v3 --depth 1`
2. `cd nadekobot && dotnet publish -c Release -o output/ src/NadekBot/`
3. `cd output && cp creds_example.yml creds.yml`
4. Open `creds.yml` with your favorite text editor
5. [Enter your bot's token](creds-guide)
6. Run the bot `dotnet NadekoBot.dll`
7. 🎉

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