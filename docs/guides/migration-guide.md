# Migration instructions (2.x to v3)

## Windows

1. Run your WizBot Updater first, and **make sure your bot is updated to at least 2.46.5**
    - **Run your 2.46.5 Bot** and make sure it works, and then **stop it**  
    - Close your old WizBot Updater
2. Get the new WizBot v3 Updater [here](https://wizbot.cc/downloads/v3)
3. Click on the + icon to add a new bot
4. Next to the path, click on the folder icon and select the folder where your 2.46.5 bot is
    - ℹ In case you're not sure where it's located, you can open your old updater and see it
5. If you've selected the correct path, you should have an **Update** button available, click it
6. You're done; you can now run your bot, and you can uninstall your old updater if you no longer have 2.x bots
7. 🎉

## Linux

1. In order to migrate a bot hosted on **Linux**, first update your current version to the latest 2.x version using the 2.x installer, run the bot, and make sure it works. Then:
    - Run the **old** installer with `cd ~ && wget -N https://github.com/Wizkiller96/WizBot-BashScript/raw/1.9/linuxAIO.sh && bash linuxAIO.sh`
    - Run option **1** again
    - You **MUST** Run the bot now to ensure database is ready for migration
    - Type `.stats` and ensure the version is `2.46.5` or later
    - Stop the bot
2. Make sure your bot's folder is called `WizBot`
    - Run `cd ~ && ls`
    - Confirm there is a folder called WizBot (not wizbot, in all lowercase)
3. Migrate your bot's data using the new installer:
    - Run the **new** installer `cd ~ && wget -N https://github.com/Wizkiller96/wizbot-bash-installer/-/raw/master/linuxAIO.sh && bash linuxAIO.sh`
    - The installer should notify you that your data is ready for migration in a message above the menu.
    - Install prerequisites (type `1` and press enter), and make sure it is successful
    - Download WizBot v3 (type `2` and press enter)
    - Run the bot (type `3` and press enter)
4. Make sure your permissions, custom reactions, credentials, and other data is preserved
    - `.stats` to ensure owner id (credentials) is correct
    - `.lcr` to see custom reactions
    - `.lp` to list permissions
5. 🎉 Enjoy. If you want to learn how to update the bot, click [here](../linux-guide/#update-instructions)

## Manual 

⚠ NOT RECOMMENDED  
⚠ WizBot v3 requires [.net 5](https://dotnet.microsoft.com/download/dotnet/5.0)

1. In order to migrate a bot hosted **on Linux or from source on Windows**
    - First update your current version to the latest 2.x version using the 2.x installer
    - Then you **must** run the bot to prepare the database for the migration, and make sure the bot works prior to upgrade.
 Then:
2. Rename your old wizbot bot folder to `wizbot_2x`
    - `mv WizBot wizbot_2x`
3. Build the new version and move old data to the output folder 
    1. Clone the v3 branch to a separate folder 
        - `git clone https://github.com/Wizkiller96/WizBot -b v3 --depth 1`
    2. Build the bot
        - `dotnet publish -c Release -o output/ src/WizBot/`
    3. Copy old data
        - ⚠ Be sure you copy the correct command for your system!
        - **Windows:** `cp -r -fo wizbot_2x/src/WizBot/data wizbot/src/WizBot/data`
        - **Linux:** `cp -rf wizbot_2x/src/WizBot/data wizbot/src/WizBot/data`
    4. Copy the database 
        - `cp wizbot_2x/src/WizBot/bin/Release/netcoreapp2.1/data/WizBot.db wizbot/output/data`
    5. Copy your credentials
        - `cp wizbot_2x/src/WizBot/credentials.json wizbot/output/`
4. Run the bot
    - `cd wizbot/output`
    - `dotnet WizBot.dll`
5. That's it. Just make sure that when you're updating the bot, you're properly backing up your old data.
