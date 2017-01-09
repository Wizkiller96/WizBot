# WizBot a Discord bot 
WizBot is written in C# and Discord.net for more information visit https://github.com/Wizkiller96/WizBot

## Install Docker
Follow the respective guide for your operating system found here https://docs.docker.com/engine/installation/

## WizBot Setup Guide
For this guide we will be using the folder /wizbot as our config root folder.

```
docker create --name=wizbot -v /wizbot/data:/opt/WizBot/src/WizBot/bin/Release/netcoreapp1.0/data -v /WizBot/credentials.json:/opt/WizBot/src/WizBot/credentials.json kwoth/wizbot:dev
```
-If you are coming from a previous version of WizBot (the old docker) make sure your crednetials.json has been copied into this directory and is the only thing in this folder. 

-If you are making a fresh install, create your credentials.json from the following guide and palce it in the /wizbot folder
http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/

Next start the docker up with 

```docker start wizbot; docker logs -f wizbot```

The docker will start and the log file will start scrolling past. Depending on hardware the bot start can take up to 5 minutes on a small DigitalOcean droplet.
Once the log ends with "WizBot | Starting WizBot v1.0-rc2" the bot is ready and can be invited to your server. Ctrl+C at this point to stop viewing the logs.

After a few moments you should be able to invite WizBot to your server. If you cannot check the log file for errors 

## Updates / Monitoring

* Upgrade to the latest version of WizBot simply `docker restart wizbot`.
* Monitor the logs of the container in realtime `docker logs -f wizbot`.

If you have any issues with the docker setup, please ask in #help but indicate you are using the docker.

For information about configuring your bot or its functionality, please check the http://wizbot.readthedocs.io/en/latest guides.
