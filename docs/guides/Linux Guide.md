## Setting up WizBot on Linux

**Setting up WizBot on Linux Digital Ocean Droplet**			
If you want WizBot to play music for you 24/7 without having to hosting it on your PC and want to keep it cheap, reliable and convenient as possible, you can try WizBot on Linux Digital Ocean Droplet using the link [DigitalOcean](http://m.do.co/c/7290047d0c84/) (and using this link will be supporting WizBot and will give you **$10 credit**)

**Operating System Compatibility**
It is recommended that you get **Ubuntu 16.04**, as there have been nearly no problems with it. Also, **32-bit systems are incompatible**.

Compatible operating systems:
- Ubuntu: 14.04, 16.04, 16.10, 17.04, 17.10
- Debian 8
- CentOS 7


**Setting up WizBot**			
Assuming you have followed the link above to setup an account and Droplet with 64bit OS in Digital Ocean and got the `IP address and root password (in email)` to login, its time to get started.

**Go through this whole guide before setting up WizBot**

#### Prerequisites
- Download [PuTTY](http://www.chiark.greenend.org.uk/~sgtatham/putty/download.html)
- Download [WinSCP](https://winscp.net/eng/download.php) *(optional)*
- Create and Invite the bot.
	- Read here how to [create a Discord Bot application and invite it.](http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/#creating-discord-bot-application)

#### Starting up

- **Open PuTTY.exe** that you downloaded before, and paste or enter your `IP address` and then click **Open**.
If you entered your Droplets IP address correctly, it should show **login as:** in a newly opened window.
- Now for **login as:**, type `root` and press enter.
- It should then, ask for password, type the `root password` you have received in your **email address registered with Digital Ocean**, then press Enter.

If you are running your VPS/ droplet for the first time, it will most likely ask you to change your VPS root password, to do that, type the **password you received through email** it won't show any changes on the screen like `******` when password is being typed, press Enter once done. 			
Type a **new password**, press Enter and type the **new password** again and you're done.			
**Write down and save the new password somewhere safe.**				
After you've done that, you are ready to use your VPS.


#### Getting WizBot
##### Part I - Downloading the installer
Use the following command to get and run `linuxAIO.sh`		
(Remember **Do Not** rename the file **linuxAIO.sh**)

`cd ~ && wget -N https://github.com/Wizkiller96/WizBot-BashScript/raw/1.9/linuxAIO.sh && bash linuxAIO.sh`

You should see these following options after using the above command:

```
1. Download WizBot
2. Run WizBot (Normally)
3. Run WizBot with Auto Restart (Run WizBot normally before using this.)
4. Auto-Install Prerequisites (For Ubuntu, Debian and CentOS)
5. Set up credentials.json (If you have downloaded WizBot already)
6. Set up pm2 for WizBot (see README)
7. Start WizBot in pm2 (complete option 6 first)
8. Exit
```
##### Part II - Downloading WizBot prerequisites

**If** you are running WizBot for the first time on your system and never had any *prerequisites* installed and have Ubuntu, Debian or CentOS, Press `4` and `enter` key, then `y` when you see the following:
```
Welcome to WizBot Auto Prerequisites Installer.
Would you like to continue?
```
That will install all the prerequisites your system need to run WizBot.			
(Optional) **If** you want to install it manually, you can try finding it [here.](https://github.com/Wizkiller96/WizBot-BashScript/blob/1.9/wizbotautoinstaller.sh)

Once *prerequisites* finish installing,

##### Part III - Installing WizBot
Choose `1` to get the **most updated build of WizBot** 

and then press `enter` key.	

When installation is complete, you will see the options again.

Next, check out:
##### Part IV - Setting up credentials

- [1. Set up credentials.json](http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/#setting-up-credentialsjson-file)
- [2. Get the Google API](http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/#setting-up-your-api-keys)

You will need the following for the next step:
![botimg](https://i.imgur.com/0HWyQkj.jpg)

- **Bot's Client ID** and **Bot's ID** (both are same) [(*required)](http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/#setting-up-credentialsjson-file)
- **Bot's Token** (not client secret) [(*required)](http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/#setting-up-credentialsjson-file)
- Your **Discord userID** [(*required)](http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/#setting-up-credentialsjson-file)
- **Google Api Key** [(optional)](http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/#setting-up-your-api-keys)
- **LoL Api Key** [(optional)](http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/#setting-up-your-api-keys)
- **Mashape Key** [(optional)](http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/#setting-up-your-api-keys)
- **Osu Api Key** [(optional)](http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/#setting-up-your-api-keys)

Once you have acquired them, press `5` to **Set up credentials.json**

You will be asked to enter the required informations, just follow the on-screen instructions and enter the required information.		
*i.e* If you are asked **Bot's Token**, then just copy and paste or type the **Bot's Token** and press `enter` key.

(If you want to skip any optional infos, just press `enter` key without typing/pasting anything.)		
Once done,		
##### Part V - Checking if WizBot is working
You should see the options again.	
Next, press `2` to **Run WizBot (Normally)**.
Check in your discord server if your new bot is working properly.	
#### Part VI - Setup, Running WizBot and Updating with [pm2](https://github.com/Unitech/pm2/blob/master/README.md) [strongly recommended]

**If you followed Part V and started WizBot, make sure to exit the bot by using `.die` if it is running in your server, and/or by pressing `8` in the console to exit.** 

You may be presented with the installer main menu from Step I. If not, simply download it again as described in the following section.

WizBot can be run using [pm2](https://github.com/Unitech/pm2), a process manager that seamlessly handles keeping your bot up. Besides this, it handles disconnections and shutdowns gracefully, ensuring any leftover processes are properly killed. It also persists on server restart, so you can restart your server or VPS/computer and pm2 will manage the startup of your bot. Lastly, there is proper error logging and overall logging. These are just a few features of pm2, and it is a great way to run WizBot with stability.

##### Setting up pm2/NodeJS for WizBot

> If you already have NodeJS and pm2 installed on your system, you can skip the *Option 6* for installing pm2 which is a one-time thing. Scroll down to see startup instructions.

There is an automated script built in the WizBot installer so installation and startup is a breeze. You may already have the `linuxAIO.sh` file downloaded from the first step, but you should download it again to keep up to date for potential changes in the installer. Download `linuxAIO.sh`:

`cd ~ && wget -N https://github.com/Wizkiller96/WizBot-BashScript/raw/1.9/linuxAIO.sh`

We can then run the script: `sudo bash linuxAIO.sh` and you will be presented with the normal WizBot installer options.

**Make sure you have installed WizBot with the first option [1] before doing this, and also have installed the prerequisites with step [4].** 

+Simply choose **Option 6** to setup pm2 and install it along with NodeJS. This will update your NodeJS so there's no harm running it even if you have NodeJS on your system. It will also install pm2 and then exit to the installer menu again.

##### Running WizBot with pm2 and Updating WizBot within pm2

Once you are done installing pm2 with NodeJS, then you can select **Option 7** which will bring you to a menu of choices. These are the normal choices you have for running WizBot. 

- [1] Start with auto-restart with `.die` and no auto-update.
- [2] Start with auto-restart with `.die` *and* auto-update on restart as well.
- [3] Run normally without any auto-restart or auto-update functionality.

Simply choose one of these and WizBot will start in pm2! If you did everything correctly, you can run the following to check your WizBot setup:

`sudo pm2 status` to see all pm2 processes

`sudo pm2 info WizBot` information about WizBot 

`sudo pm2 logs WizBot` to view real-time logs of WizBot (you can do `sudo pm2 logs WizBot --lines <number>`) (number = how many lines you wish to output) for viewing more lines of the log. The logfile is also stored and presented at the top of these commands.

> **Updating WizBot within pm2:**

**If you don't auto-update WizBot and manually do, one simply needs to run the linuxAIO.sh script: `sudo sh linuxAIO.sh` as we normally would and choose [1] Download WizBot, then after downloading is complete/build is done, just `pm2 restart WizBot` -- that's all!** 

**NOTE:**  If you did the pm2 setup, you are done! You do not need to follow the startup instructions later in the guide, as we've used the script and pm2 to start WizBot up already. Wasn't that easy? :-)


**Some other useful pm2 commands:**

`sudo pm2 startup && sudo pm2 save` will setup pm2 to persist even on system reboot by saving the process ID information as a system service. Just need to do this once if you wish.

`sudo pm2 stop WizBot` will stop WizBot properly and ensure it is shut down. `sudo pm2 restart WizBot` will restart WizBot properly as well, shutting it down first and promptly restarting.


This is the recommended way to keep WizBot running smoothly.

#### Part VII - Running WizBot on tmux [if you wish not to use pm2]

If your bot is working properly in your server, type `.die` to **shut down the bot**, then press `8` on the console to **exit**.

Next, [Run your bot again with **tmux**.](http://wizbot.readthedocs.io/en/latest/guides/Linux%20Guide/#running-wizbot)	

[Check this when you need to **restart** your **WizBot** anytime later along with tmux session.](http://wizbot.readthedocs.io/en/latest/guides/Linux%20Guide/#restarting-wizbot)

#### Running WizBot

**Create a new Session:**

- `tmux new -s wizbot`  
  
The above command will create a new session named **wizbot** *(you can replace “wizbot” with anything you prefer and remember its your session name)* so you can run the bot in background without having to keep the PuTTY running.

**Next, we need to run `linuxAIO.sh` in order to get the latest running scripts with patches:**

- `cd ~ && wget -N https://github.com/Wizkiller96/WizBot-BashScript/raw/1.9/linuxAIO.sh && bash linuxAIO.sh`

**From the options,**

Choose `2` to **Run WizBot normally.**		
**NOTE:** With option `2` (Running normally), if you use `.die` [command](http://wizbot.readthedocs.io/en/latest/Commands%20List/#administration) in discord. The bot will shut down and will stay offline until you manually run it again. (best if you want to check the bot.)

Choose `3` to **Run WizBot with Auto Restart.**	
**NOTE:** With option `3` (Running with Auto Restart), bot will auto run if you use `.die` [command](http://wizbot.readthedocs.io/en/latest/Commands%20List/#administration) making the command `.die` to function as restart.	

It will show you the following options: 
```
1. Run Auto Restart normally without Updating.
2. Run Auto Restart and update WizBot.
3. Exit
```

- With option `1. Run Auto Restart normally without Updating.` Bot will restart on `die` command and will not be downloading the latest build available.
- With option `2. Run Auto Restart and update WizBot.` Bot will restart and download the latest build of bot available everytime `die` command is used.

**Remember** that, while running with Auto Restart, you will need to [close the tmux session](http://wizbot.readthedocs.io/en/latest/guides/Linux%20Guide/#restarting-wizbot) to stop the bot completely.

**Now check your Discord, the bot should be online**

Next to **move the bot to background** and to do that, press **CTRL+B, release, D** (that will detach the  session using TMUX) and you can finally close **PuTTY**.

#### Restarting WizBot

**Restarting WizBot:**

**If** you have chosen option `2` to **Run WizBot with Auto Restart** from WizBot's `linuxAIO.sh` *[(you got it from this step)](http://wizbot.readthedocs.io/en/latest/guides/Linux%20Guide/#getting-wizbot)*	
You can simply type `.die` in the server you have your WizBot to make her restart.

**Restarting WizBot with the Server:**

Open **PuTTY** and login as you have before, type `reboot` and press Enter.

**Restarting Manually:**

- Kill your previous session, check with `tmux ls`
- `tmux kill-session -t wizbot` (don't forget to replace "wizbot" to what ever you named your bot's session)
- [Run the bot again.](http://wizbot.readthedocs.io/en/latest/guides/Linux%20Guide/#running-wizbot)

#### Updating WizBot

- Connect to the terminal through **PuTTY**.
- `tmux kill-session -t wizbot` (don't forget to replace **wizbot** in the command with the name of your bot's session)
- Make sure the bot is **not** running.
- `tmux new -s wizbot` (**wizbot** is the name of the session)
- `cd ~ && wget -N https://github.com/Wizkiller96/WizBot-BashScript/raw/1.9/linuxAIO.sh && bash linuxAIO.sh`
- Choose `1` to update the bot with **latest build** available.
- Next, choose either `2` or `3` to run the bot again with **normally** or **auto restart** respectively.
- Done.

#### Additional Information

##### Setting up Music

To set up WizBot for music and Google API Keys, follow [Setting up your API keys.][setup music]

##### tmux

- If you want to **see the sessions** after logging back again, type `tmux ls`, and that will give you the list of sessions running.
- If you want to **switch to/ see that session**, type `tmux a -t wizbot` (**wizbot** is the name of the session we created before so, replace **“wizbot”** with the session name you created.)
- If you want to **kill** WizBot **session**, type `tmux kill-session -t wizbot`

##### Setting up SFTP

- Open **WinSCP**
- Click on **New Site** (top-left corner).
- On the right-hand side, you should see **File Protocol** above a drop-down selection menu.
- Select **SFTP** *(SSH File Transfer Protocol)* if its not already selected.
- Now, in **Host name:** paste or type in your `Digital Ocean Droplets IP address` and leave `Port: 22` (no need to change it).
- In **Username:** type `root`
- In **Password:** type `the new root password (you changed at the start)`
- Click on **Login**, it should connect.
- It should show you the WizBot folder which was created by git earlier on the right-hand side window.
- Open that folder, then open the `src` folder, followed by another `WizBot` folder and you should see `credentials.json` there.

##### Setting up credentials.json

- Copy the `credentials.json` to desktop
- EDIT it as it is guided here: [Setting up credentials.json][setup credentials]
- Paste/put it back in the folder once done. `(Using WinSCP)`
- **If** you already have WizBot 1.3.x setup and have `credentials.json` and `WizBot.db`, you can just copy and paste the `credentials.json` to `WizBot/src/WizBot` and `WizBot.db` to `WizBot/src/WizBot/bin/Release/netcoreapp2.0/data` using WinSCP.			
**Or** follow the [Upgrading Guide.][upgrading]


[img7]: https://cdn.discordapp.com/attachments/251504306010849280/251505766370902016/setting_up_credentials.gif
[setup credentials]: http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/#setting-up-credentialsjson-file
[setup music]: http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/#setting-up-your-api-keys
[upgrading]: http://wizbot.readthedocs.io/en/latest/guides/Upgrading%20Guide/