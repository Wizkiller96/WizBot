## Migration from 2.x 

⚠ If you're already hosting NadekoBot, You **MUST** update to latest version of 2.x and **run your bot at least once** before switching over to v3.

#### Windows migration instructions

1. Run your NadekoBot Updater first, and **make sure your bot is updated to at least 2.46.5**
2. Get the new NadekoBot Updater [here](https://dl.nadeko.bot)
3. Click on the + icon to add a new bot
4. Next to the path, click on the folder icon and select the folder where your 2.46.5 bot is
   - ℹ In case you're not sure where it's located, you can open your old updater and see it
5. If you've selected the correct path, you should have an **Update** button available, click it
6. You're done; you can now run your bot, and you can uninstall your old updater if you no longer have 2.x bots
7. 🎉

#### Linux migration instructions

1. In order to migrate a bot hosted on **Linux**, first update your current version to the latest 2.x version using the 2.x installer, run the bot, and make sure it works. Then:
   - Run the **old** installer with `cd ~ && wget -N https://github.com/Kwoth/NadekoBot-BashScript/raw/1.9/linuxAIO.sh && bash linuxAIO.sh`
   - Run option **1** again
   - Run the bot
   - Type `.stats` and ensure the version is `2.46.5` or later
   - Stop the bot
2. Make sure your bot's folder is called `NadekoBot`
   - Run `cd ~ && ls`
   - Confirm there is a folder NadekoBot
3. Migrate your bot's data using the new installer:
   - Run the **new** installer `cd ~ && wget -N https://gitlab.com/Kwoth/nadeko-bash-installer/-/raw/master/linuxAIO.sh && bash linuxAIO.sh`
   - The installer should notify you that your data is ready for migration in a message above the menu
   - Install prerequisites (type `1` and press enter), and make sure it is successful
   - Download NadekoBot v3 (type `2` and press enter)
   - Run the bot (type `3` and press enter)
4. Make sure your permissions, custom reactions, credentials, and other data is preserved
   - `.stats` to ensure owner id (credentials) is correct
   - `.lcr` to see custom reactions
   - `.lp` to list permissions
5. 🎉 Enjoy. If you want to learn how to update the bot, click (here)[#linux-updating-the-bot].

#### Manual migration 

⚠ NOT RECOMMENDED  
⚠ NadekoBot v3 requires .net 5

##### Windows

1. In order to migrate a bot hosted **from source on Windows**, first update your current version to the latest 2.x version using the 2.x installer, run the bot, and make sure it works. Then:
2. Rename your old nadeko bot folder to `nadekobot_2x`
   - `mv NadekoBot nadekobot_2x`
3. Build the new version and move old data to the output folder 
   1. Clone the v3 branch to a separate folder 
      - `git clone https://gitlab.com/kwoth/nadekobot -b v3 --depth 1`
   2. Build the bot
      - `dotnet publish -c Release -o output/ src/NadekoBot/`
   3. Copy old data
      - `cp -r -fo nadekobot_2x/src/NadekoBot/data nadekobot/src/NadekoBot/data`
   4. Copy the database 
      - `cp nadekobot_2x/src/NadekoBot/bin/Release/netcoreapp2.1/data/NadekoBot.db nadekobot/output/data`
   5. Copy your credentials
      - `cp nadekobot_2x/src/NadekoBot/credentials.json nadekobot/output/`
4. Run the bot
   - `cd nadekobot/output`
   - `dotnet NadekoBot.dll`
5. That's it. Just make sure that when you're updating the bot, you're properly backing up your old data.

##### Linux

1. In order to migrate a bot hosted on **Linux**, first update your current version to the latest 2.x version using the 2.x installer, run the bot, and make sure it works. Then:
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
   5. Copy your credentials
      - `cp nadekobot_2x/src/NadekoBot/credentials.json nadekobot/output/`
4. Run the bot
   - `cd nadekobot/output`
   - `dotnet NadekoBot.dll`
5. That's it. Just make sure that when you're updating the bot, you're properly backing up your old data.

## Fresh Installation

- [Windows - Release](#windows-release)
- [Windows - From Source](#windows-from-source)
- [Linux - From Source](#linux-from-source)
- [Linux - Release](#linux-release)
- [Docker]

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

### Linux From Source 

Open Terminal (if you're on an installation with a window manager) and navigate to the location where you want to install the bot (for example `cd ~`) 

##### Installation Instructions

1. Download and run the **new** installer script `cd ~ && wget -N https://gitlab.com/Kwoth/nadeko-bash-installer/-/raw/master/linuxAIO.sh && bash linuxAIO.sh`
2. Install prerequisites (type `1` and press enter)
3. Download the bot (type `2` and press enter)
4. Exit the installer in order to set up your `creds.yml` 
5. Copy the creds.yml template `cp nadekobot/output/creds_example.yml nadekobot/output/creds.yml` 
6. Open `nadekobot/output/creds.yml` with your favorite text editor. We will use nano here
   - `nano nadekobot/output/creds.yml`
7. [Enter your bot's token](#creds-guide)
   - After you're done, you can close nano (and save the file) by inputting, in order 
      - `CTRL`+`X`
      - `Y`
      - `Enter`
8. Run the bot (type `3` and press enter)

##### Update Instructions

1. ⚠ Stop the bot
2. Update and run the **new** installer script `cd ~ && wget -N https://gitlab.com/Kwoth/nadeko-bash-installer/-/raw/master/linuxAIO.sh && bash linuxAIO.sh`
3. Update the bot (type `2` and press enter)
4. Run the bot (type `3` and press enter)
5. 🎉 

### Linux Release

##### Installation Instructions

1. Download the latest release from <https://gitlab.com/Kwoth/nadekobot/-/releases>
   - Look for the file called "X.XX.X-linux-x64-build.tar" (where X.XX.X is a series of numbers) and download it
2. Untar it 
   ⚠ Make sure that you change X.XX.X to the same series of numbers as in step 1!
   - `tar xf X.XX.X-linux-x64-build.tar`
3. Rename the `nadekobot-linux-x64` to `nadekobot` 
   - `mv nadekobot-linux-x64 nadekobot`
4. Move into nadekobot directory and make NadekoBot executable
   - `cd nadekobot && chmod +x NadekoBot`
5. Copy the creds.yml template 
   - `cp creds_example.yml creds.yml` 
6. Open `creds.yml` with your favorite text editor. We will use nano here
   - `nano nadekobot/output/creds.yml`
8. [Enter your bot's token](#creds-guide)
   - After you're done, you can close nano (and save the file) by inputting, in order 
      - `CTRL`+`X`
      - `Y`
      - `Enter`
9. Run the bot
   - `./NadekoBot`

##### Update Instructions

1. Stop the bot
2. Download the latest release from <https://gitlab.com/Kwoth/nadekobot/-/releases>
   - Look for the file called "X.XX.X-linux-x64-build.tar" (where X.XX.X is a series of numbers) and download it
3. Untar it 
   ⚠ Make sure that you change X.XX.X to the same series of numbers as in step 2!
   - `tar xf 2.99.8-linux-x64-build.tar`
4. Rename the old nadekobot directory to nadekobot-old (remove your old backup first if you have one, or back it up under a different name)
   - `rm -rf nadekobot-old 2>/dev/null`
   - `mv nadekobot nadekobot-old`
5. Rename the new nadekobot directory to nadekobot
   - `mv nadekobot-linux-x64 nadekobot`
6. Remove old strings and aliases to avoid overwriting the updated versions of those files  
   ⚠ If you've modified said files, back them up instead
   - `rm nadekobot-old/data/aliases.yml`
   - `rm -r nadekobot-old/data/strings`
7. Copy old data
   - `cp -RT nadekobot-old/data/ nadekobot/data/`
8. Copy creds.yml
   - `cp nadekobot-old/creds.yml nadekobot/`
9. Move into nadekobot directory and make the NadekoBot executable
   - `cd nadekobot && chmod +x NadekoBot`
10. Run the bot 
   - `./NadekoBot`

🎉 Enjoy

##### Steps 3 - 9 as a single command  

Don't forget to change X.XX.X to match step 2.
```sh
tar xf X.XX.X-linux-x64-build.tar && \
rm -rf nadekobot-old 2>/dev/null && \
mv nadekobot nadekobot-old && \
mv nadekobot-linux-x64 nadekobot && \
rm nadekobot-old/data/aliases.yml && \
rm -r nadekobot-old/data/strings && \
cp -RT nadekobot-old/data/ nadekobot/data/ && \
cp nadekobot-old/creds.yml nadekobot/ && \
cd nadekobot && chmod +x NadekoBot
```


## Creds Guide

This document aims to guide you through the process of creating a Discord account for your bot 
(the Discord Bot application), and inviting that account into your Discord server.

![Create a bot application and copy token to creds.yml file](https://cdn.nadeko.bot/tutorial/bot-creds-guide.gif)

- Go to [the Discord developer application page][DiscordApp].
- Log in with your Discord account.
- Click **New Application**.
- Fill out the `Name` field however you like.
- Go to the **Bot** tab on the left sidebar.
- Click on the `Add a Bot` button and confirm that you do want to add a bot to this app.
- **Optional:** Add bot's avatar and description.
- Copy your Token to `creds.yml` as shown above.
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

That's it! You may now go back to the installation guide you were following before 🎉

[DiscordApp]: https://discordapp.com/developers/applications/me
[ffmpeg-32bit]: https://cdn.nadeko.bot/dl/ffmpeg-32.zip
[ffmpeg-64bit]: https://cdn.nadeko.bot/dl/ffmpeg-64.zip
[youtube-dl]: https://yt-dl.org/downloads/latest/youtube-dl.exe
[docs]: https://nadekobot.rtfd.io
