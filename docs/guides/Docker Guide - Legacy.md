# Setting up WizBot on Docker
WizBot is written in C# and Discord.Net for more information visit <https://github.com/Kwoth/WizBot>

## Before you start ...

... If your PC falls under any of the following cases, please grab Docker Toolbox instead.

For Windows [[Download Link](https://download.docker.com/win/stable/DockerToolbox.exe)]
- Any Windows version without Hyper-V Support
- Windows 10 Home Edition
- Windows 8 and earlier

For Mac [[Download Link](https://download.docker.com/mac/stable/DockerToolbox.pkg)]
- Any version between 10.8 “Mountain Lion” and 10.10.2 "Yosemite"

## Prerequisites
- [Docker](https://store.docker.com/search?type=edition&offering=community) or [Docker Toolbox](https://www.docker.com/products/docker-toolbox).
- [Create Discord Bot application](http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/#creating-discord-bot-application) and [Invite the bot to your server](http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/#inviting-your-bot-to-your-server). 
- Have your [credentials.json](http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/#setting-up-your-credentials) in your home folder. To go to your home folder on ...
- Linux: **cd ~**
- Mac: **⌘ + Shift + H**
- Windows: Enter **%userprofile%** in your address bar

## Fool-proof Quick start guide - Just want to get things working

Just copy everything down below (in one block of text), and paste it to your console, and it should perform it's magic on its own.

```
cd ~
docker pull wizkiller96/wizdecker:latest
docker stop wizbot
docker cp wizbot:/root/WizBot/credentials.json credentials.json
docker cp wizbot:/opt/WizBot/src/WizBot/bin/Release/netcoreapp2.1/data/WizBot.db WizBot.db
docker rm wizbot
docker create --name=wizbot -v /wizbot/conf:/root/wizbot -v /wizbot/data/:/opt/WizBot/src/WizBot/bin/Release/netcoreapp2.1/data wizkiller96/wizdecker:latest
docker cp credentials.json wizbot:/root/wizbot
docker cp WizBot.db wizbot:/opt/WizBot/src/WizBot/bin/Release/netcoreapp2.1/data/WizBot.db
docker start -a wizbot
```

First time install might encounter a few errors along the way (Namely step 2, 3, 4, 5, 8), this is to be expected, as you do not have the settings/files set up.

## Step-by-step Explanation

### 0. Going to the home directory

**Command:** `cd ~`

There has been an increase of users who's default folder is not set on the windows user folder, hence by doing this way, it'll force everyone at the same location.

### 1. Grabbing the latest build

**Command:** `docker pull wizkiller96/wizdecker:latest`

This will grab the latest WizBot Docker image file from the internet and get ready to be used later.

### 2. Stopping any existing WizBot container

**Command:** `docker stop wizbot`

This will stop previously running docker container (if exist)

### 3. Backup your credentials.json file

**Command:** `docker cp wizbot:/root/wizbot/credentials.json credentials.json`

Technically speaking, you do not need to run this. But for the sake of fool-proof, this would make a copy of the credentials.json from the docker container and put it to your home folder.

### 4. Backup your WizBot.db file

**Command:** `docker cp wizbot:/opt/WizBot/src/WizBot/bin/Release/netcoreapp2.1/data/WizBot.db WizBot.db`

Again, you most likely do not need to run this. But for the sake of fool-proof, this would make a copy of the WizBot.db from the docker container and put it to your home folder.

### 5. Remove the current WizBot container

**Command:** `docker rm wizbot`

This will delete the bot container, along with any of its settings inside. (That's why we made the backup of the two important files above)

### 6. Creating a new WizBot container with updated files

**Command:** `docker create --name=wizbot -v /wizbot/conf:/root/wizbot -v /wizbot/data/:/opt/WizBot/src/WizBot/bin/Release/netcoreapp2.1/data wizkiller96/wizdecker:latest`

This command will build a new wizbot container based on the files we've pulled from **__Step 1__**.

And it will link two folders from your local drive and store the data within. Namely your **__credentials.json__**, which is saved under **__/wizbot/conf__**,  and **__WizBot.db__**, which is saved under **__/wizbot/data__**.

However, in the case if you did not create the folders before hand, or if you were using Windows and did not set up permission right, no files will be generated. (This is why there's the fool-proof steps 3, 4, 7 and 8)

### 7. Copy credentials.json file back into the container

**Command:** `docker cp credentials.json wizbot:/root/wizbot`

Technically speaking, if the file exists in /wizbot/conf, then you do not need to run this. But for the sake of fool-proof, this command makes a copy of the credentials.json from your home folder and it'll be placed in the docker container.

### 8. Copy WizBot.db database back into the container

**Command:** `docker cp WizBot.db wizbot:/opt/WizBot/src/WizBot/bin/Release/netcoreapp2.1/data/WizBot.db`

As I've been saying, this is yet another redundent step, just to make the whole thing fool-proof. This command copies the database with all the user info (such as the currency, experience, level, waifus, etc) and put it into the container.

### 9. Start the bot!

**Command:** `docker start -a wizbot`

This would start the bot and attach the output of the bot on screen, similiar to you running `docker logs -f wizbot` after the bot has started.

### Additional Info
If you have any issues with the docker setup, please ask in #help channel on our [Discord server](https://discord.gg/0YNaDOYuD5QOpeNI) but indicate you are using the docker.

For information about configuring your bot or its functionality, please check the [documentation](http://wizbot.readthedocs.io/en/latest).
