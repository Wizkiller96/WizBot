#### If you have WizBot 1.x on Windows

**If you have WizBot 1.x**

- Follow the [Windows Guide](http://wizbot.readthedocs.io/en/latest/guides/Windows%20Guide/) and install the latest version of **WizBot**.
- Navigate to your **old** `WizBot` folder and copy your `credentials.json` file and the `data` folder.
- Paste credentials into the **WizBot 1.4x+** `C:\Program Files\WizBot\system` folder.
- Paste your **old** `WizBot` data folder into **WizBot 1.4x+** `C:\Program Files\WizBot\system` folder.
- If it asks you to overwrite files, it is fine to do so.
- Next launch your **new** WizBot as the guide describes, if it is not already running.


#### If you are running Dockerised WizBot

- Shutdown your existing container **docker stop wizbot**.
- Move you credentials and other files to another folder.
- Delete your container **docker rm wizbot**.
- Create a new container **docker create --name=wizbot -v /wizbot/:/root/wizbot uirel/wizbot:1.4**.
- Start the container **docker start wizbot** wait for it to complain about lacking credentials.
- Stop the container **docker stop wizbot** open the WizBot folder and replace the credentials, database and other files with your copies.
- Restart the container **docker start wizbot**.

#### If you have WizBot 1.x on Linux or MacOS

- Backup the `WizBot.db` from `WizBot/src/WizBot/bin/Release/netcoreapp1.0/data`
- Backup the `credentials.json` from `WizBot/src/WizBot/`
- **For MacOS Users Only:** download and install the latest version of [.NET Core SDK](https://www.microsoft.com/net/core#macos)
- Next, use the command `cd ~ && wget -N https://github.com/Wizkiller96/WizBot-BashScript/raw/1.4/linuxAIO.sh && bash linuxAIO.sh`
- **For Ubuntu, Debian and CentOS Users Only:** use the option `4. Auto-Install Prerequisites` to install the latest version of .NET Core SDK.
- Use option `1. Download WizBot` to update your WizBot to 1.4.x.
- Next, just [run your WizBot.](http://wizbot.readthedocs.io/en/latest/guides/Linux%20Guide/#running-wizbot)
- *NOTE: 1.4.x uses `WizBot.db` file from `WizBot/src/WizBot/bin/Release/netcoreapp1.1/data` folder.*