# Setting up WizBot on Docker
WizBot is written in C# and Discord.Net for more information visit <https://github.com/Kwoth/WizBot>

#### Prerequisites
- [Docker](https://docs.docker.com/engine/installation/)
- [Create Discord Bot application](http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/#creating-discord-bot-application) and [Invite the bot to your server](http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/#inviting-your-bot-to-your-server). 

#### Setting up the container
For this guide we will be using the folder /wizbot as our config root folder.
```
docker create --name=wizbot -v /wizbot/conf/:/root/wizbot -v /wizbot/data:/opt/WizBot/src/WizBot/bin/Release/netcoreapp1.1/data uirel/wizbot:1.4
```

#### Moving `credentials.json` into the docker container. 

- If you are coming from a previous version of wizbot (the old docker) make sure your credentials.json has been copied into this directory and is the only thing in this folder.
- If you are making a fresh install, create your credentials.json from the following guide and place it in the /wizbot folder [WizBot JSON Guide](http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/). 
- To copy the the file from your computer to a container: 
```
docker cp /Directory/That/Contains/Your/credentials.json wizbot:/credentials.json
```

#### Start up docker
```
docker start wizbot; docker logs -f wizbot
```
The docker will start and the log file will start scrolling past. This may take a long time. The bot start can take up to 5 minutes on a small DigitalOcean droplet.
Once the log ends with "WizBot | Starting WizBot vX.X" the bot is ready and can be invited to your server. Ctrl+C at this point if you would like to stop viewing the logs.

After a few moments, WizBot should come online on your server. If it doesn't, check the log file for errors. 

#### Monitoring
**To monitor the logs of the container in realtime** 
```
docker logs -f wizbot
```

### Updates

#### Manual
Updates are handled by pulling the new layer of the Docker Container which contains a pre compiled update to WizBot.
The following commands are required for the default options

`docker pull uirel/wizbot:latest`

`docker stop wizbot; docker rm wizbot`

```
docker create --name=wizbot -v /wizbot/conf/:/root/wizbot -v /wizbot/data:/opt/WizBot/src/WizBot/bin/Release/netcoreapp1.1/data uirel/wizbot:1.4
```

`docker start wizbot`


#### Automatic
Automatic update are handled by [WatchTower](https://github.com/CenturyLinkLabs/watchtower).
To setup WatchTower to keep WizBot up-to-date for you with the default settings, use the following command:

```bash
docker run -d --name watchtower -v /var/run/docker.sock:/var/run/docker.sock centurylink/watchtower --cleanup wizbot --interval 300
```

This will check for updates to the docker every 5 minutes and update immediately. To check in different intervals, change `X`. X is the amount of time, in seconds. (e.g 21600 for 6 hours)

### Additional Info
For information about configuring your bot or its functionality, please check the [documentation](http://wizbot.readthedocs.io/en/latest).