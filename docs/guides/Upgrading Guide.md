##Upgrading WizBot from an older release

**If you have WizBot 1.x**

- Follow the [Windows Guide](http://wizbot.readthedocs.io/en/latest/guides/Windows%20Guide/)/[Linux Guide](http://wizbot.readthedocs.io/en/latest/guides/Linux%20Guide/)/[OS X Guide](http://wizbot.readthedocs.io/en/latest/guides/OSX%20Guide/) and install the latest version of **WizBot**.
- Navigate to your **old** `WizBot` folder and copy your `credentials.json` file and the `data` folder.
- Paste credentials into the **WizBot 1.4x+** `C:\Program Files\WizBot\system` folder.
- Paste your **old** `WizBot` data folder into **WizBot 1.4x+** `C:\Program Files\WizBot\system` folder.
- If it asks you to overwrite files, it is fine to do so.
- Next launch your **new** WizBot as the guide describes, if it is not already running.


**If you are running Dockerised WizBot**
 +- Shutdown your existing container **docker stop wizbot**
 +- Move you credentials and other files to another folder
 +- Delete your container **docker rm wizbot**
 +- Create a new container **docker create --name=wizbot -v /wizbot/:/root/wizbot wizkiller96/wizbot:dev**
 +- Start the container **docker start wizbot** wait for it to complain about lacking credentials
 +- Stop the container **docker stop wizbot** open the WizBot folder and replace the crednetials, database and other files with your copies
 +- Restart the container **docker start wizbot**