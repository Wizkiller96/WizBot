# WizBot a Discord bot 
WizBot is written in C# and Discord.net for more information visit https://github.com/Wizkiller96/WizBot

## Install Docker
Follow the respective guide for your operating system found here https://docs.docker.com/engine/installation/

## WizBot Setup Guide
For this guide we will be using the folder /wizbot as our config root folder.

```
docker create --name=wizbot -v /wizbot/data:/opt/WizBot/src/WizBot/bin/Release/netcoreapp1.0/data -v /WizBot/credentials.json:/opt/WizBot/src/WizBot/credentials.json wizkiller96/wizbot:dev
```
-If you are coming from a previous version of WizBot (the old docker) make sure your crednetials.json has been copied into this directory and is the only thing in this folder. 

-If you are making a fresh install, create your credentials.json from the following guide and palce it in the /wizbot folder
http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/

Next start the docker up with 

```docker start wizbot; docker logs -f wizbot```

The docker will start and the log file will start scrolling past. Depending on hardware the bot start can take up to 5 minutes on a small DigitalOcean droplet.
Once the log ends with "WizBot | Starting WizBot v1.0-rc2" the bot is ready and can be invited to your server. Ctrl+C at this point to stop viewing the logs.

After a few moments you should be able to invite WizBot to your server. If you cannot check the log file for errors 

## Monitoring

* Monitor the logs of the container in realtime `docker logs -f wizbot`.

## Updates

* Manual
Updates are handled by pulling the new layer of the Docker Container which contains a pre compiled update to WizBot.
The following commands are required for the default options
1. ```docker pull wizkiller96/wizbot:dev```
2. ```docker stop wizbot; docker rm wizbot```
3. ```docker create --name=wizbot -v /wizbot/data:/opt/WizBot/src/WizBot/bin/Release/netcoreapp1.0/data -v /wizbot/credentials.json:/opt/WizBot/src/WizBot/credentials.json wizkiller96/wizbot```
4. ```docker start wizbot```

* Automatic Updates
Automatic update are now handled by watchertower https://github.com/CenturyLinkLabs/watchtower
To setup watchtower to keep wizbot up-to-date for you with the default settings use the following command
```docker run -d --name watchtower -v /var/run/docker.sock:/var/run/docker.sock centurylink/watchtower --cleanup wizbot```
This will check for updates to the docker every 5 minutes and update immediately. Alternatively using the ```--interval X``` command to change the interval, where X is the amount of time in seconds to wait. eg 21600 for 6 hours.



If you have any issues with the docker setup, please ask in #help but indicate you are using the docker.

For information about configuring your bot or its functionality, please check the http://wizbot.readthedocs.io/en/latest guides.
