# Setting up WizBot on Linux

| Table of Contents |
| :--- |
| [Getting Started](#getting-started) |
| [Downloading and Installing the Prerequisites](#downloading-and-installing-the-prerequisites) |
| [Installing WizBot](#installing-wizbot) |
| [Setting up, Running and Updating WizBot with pm2](#setting-up-running-and-updating-wizbot-with-pm2-strongly-recommended) |
| [Running WizBot on tmux](#running-wizbot-on-tmux-if-you-dont-want-to-use-pm2) |
| [Making WizBot persist upon system restarts \(tmux\)](#making-wizbot-persist-upon-system-restarts-tmux-for-advanced-users) |
| [Setting up WizBot on a VPS \(Digital Ocean\)](#setting-up-wizbot-on-a-linux-vps-digital-ocean-droplet) |
| [Setting up WinSCP](#setting-up-winscp) |

### Operating System Compatibility

It is recommended that you use **Ubuntu 16.04**, as there have been nearly no problems with it. Music features are currently not working on CentOS. Also, **32-bit systems are incompatible**.

#### Compatible operating systems:

* Ubuntu: 14.04, 16.04, 16.10, 17.04, 17.10, 18.04
* Mint: 17, 18
* Debian: 8, 9
* CentOS: 7

### Getting Started

* Use the following command to get and run the `linuxAIO.sh` installer        
  * \(PS: **Do Not** rename the `linuxAIO.sh` file\)

`cd ~ && wget -N https://github.com/Wizkiller96/WizBot-BashScript/raw/1.9/linuxAIO.sh && bash linuxAIO.sh`

You should see the main menu with the following options:

```text
1. Download WizBot
2. Run WizBot (Normally)
3. Run WizBot with Auto Restart (Run WizBot normally before using this.)
4. Auto-Install Prerequisites (For Ubuntu, Debian and CentOS)
5. Set up credentials.json (If you have downloaded WizBot already)
6. Set up pm2 for WizBot (see README)
7. Start WizBot in pm2 (complete option 6 first)
8. Exit
```

### Downloading and Installing the Prerequisites

* **If** you are running WizBot for the first time on your system and never had any _prerequisites_ installed, press `4` and `enter` key, then `y` once you see the following:

  ```text
  Welcome to WizBot Auto Prerequisites Installer.
  Would you like to continue?
  ```

* That will install all prerequisites your system needs in order to run WizBot.            
  * \(Optional\) **If** you prefer to install them manually, you can try finding them [here](https://github.com/Wizkiller96/WizBot-BashScript/blob/1.9/wizbotautoinstaller.sh).

Once it finishes, the installer should automatically take you back to the main menu.

### Installing WizBot

* Choose Option `1` to get the **most updated build of WizBot**. When installation is complete, you will see the options again.
* If you haven't [set up your Discord bot application](http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/#creating-discord-bot-application) and [invited the bot to your server](http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/#inviting-your-bot-to-your-server) yet, do it now.
  * Only the ClientID, Bot Token and OwnerID are required. Everything else is optional.
  * The Google API Key is required if you want Nadeko to play music.
* Once you have acquired them, choose Option `5` to set up your credentials.
  * You will be asked to enter your credentials. Just follow the on-screen instructions and enter them as requested. \(_i.e._ If you are asked to insert the **Bot's Token**, then just copy and paste the **Bot's Token** and hit `Enter`. Rinse and repeat until it's over.\)
  * If you want to skip any optional information, just press `Enter` without typing/pasting anything.

Once you're done with the credentials, you should be taken back to the main menu.

#### Checking if WizBot is working

* Choose Option `2` to **Run WizBot \(Normally\)**.
* Check in your Discord server if your new bot is working properly. Once you're done testing, type `.die` to shut it down and return to the main menu.

You can now choose Option `3` and have WizBot run with auto restart. It will work just fine, however it's strongly advised that you use WizBot with a process manager like pm2 or tmux, as they will keep WizBot running in the background, freeing up your terminal for other tasks.

### Setting up, Running and Updating WizBot with [pm2](https://github.com/Unitech/pm2/blob/master/README.md) \[strongly recommended\]

WizBot can be run using [pm2](https://github.com/Unitech/pm2), a process manager that seamlessly handles keeping your bot up. Besides, it handles disconnections and shutdowns gracefully, ensuring any leftover processes are properly killed. It also persists on server restart, so you can restart your server or computer and pm2 will manage the startup of your bot. Lastly, there is proper error logging and overall logging. These are just a few features of pm2, and it is a great way to run WizBot with stability.

#### Setting up pm2/NodeJS for WizBot

**Before proceeding, make sure your bot is not running by either running** `.die` **in your Discord server or exiting the process with** `Ctrl+C`**.**

You may be presented with the installer main menu once you shut your bot down. If not, simply run `bash linuxAIO.sh`.

* Run Option `6` to install NodeJS and pm2.
  * If you already have NodeJS and pm2 installed on your system, you can skip this step \(which is a one-time thing\).
* There is an automated script built in the installer so installation and startup is a breeze. Just select Option `7` to bring you to a menu of choices. These are the normal choices you have for running WizBot.

  ```text
  [1] Start with auto-restart with .die and no auto-update.
  [2] Start with auto-restart with .die and auto-update on restart as well.
  [3] Run normally without any auto-restart or auto-update functionality.
  ```

* Simply choose one of these and WizBot will start in pm2! If you did everything correctly, you can run the following to check your WizBot setup:

`sudo pm2 status` to see all pm2 processes

`sudo pm2 info WizBot` information about WizBot

`sudo pm2 logs WizBot` to view real-time logs of WizBot, or

`sudo pm2 logs WizBot --lines number` \(**number** = how many lines you wish to output\) to see a specific amount of lines of the log. The logfile is also stored and presented at the top of these commands

#### Updating WizBot with pm2

* If you have set up WizBot with auto-update, simply run `.die` on your Discord server. That's it!
* If you have set up WizBot with **no** auto-update:
  * Shut your bot down with `sudo pm2 stop WizBot`
  * Open the installer with `bash linuxAIO.sh` and choose Option `1`
  * Once it's done, exit the installer with Option `8` and run `sudo pm2 restart WizBot`
    * You can watch your bot going online with `sudo pm2 logs WizBot`

### Running WizBot on tmux \[if you don't want to use pm2\]

**Before proceeding, make sure your bot is not running by either running** `.die` **in your Discord server or exiting the process with** `Ctrl+C`**.** If you are presented with the installer main menu, exit it by choosing Option `8`.

* Create a new session: `tmux new -s WizBot`  

The above command will create a new session named **WizBot** _\(you can replace “WizBot” with anything you prefer, it's your session name\)_.

* Run the installer: `bash linuxAIO.sh`
* Choose `2` to **Run WizBot normally.**
  * **NOTE**: With this option, if you use `.die` in Discord, the bot will shut down and stay offline until you manually run it again.
* Choose `3` to **Run WizBot with Auto Restart.**
  * **NOTE**: With this option, the bot will auto run if you use `.die`, making it to function as restart.

You will be shown the following options:

```text
1. Run Auto Restart normally without Updating.
2. Run Auto Restart and update WizBot.
3. Exit
```

* With option `1. Run Auto Restart normally without Updating`, the bot will restart on `.die` command and will not download the latest build available.
* With option `2. Run Auto Restart and update WizBot`, the bot will restart and download the latest build available everytime the `.die` command is used.

**Now check your Discord server, the bot should be online**

* To move the bot to the background, press **Ctrl+B**, release the keys then hit **D**. That will detach the session, allowing you to finally close the terminal window and not worry about having your bot shut down in the process.

### Updating WizBot

* If you're running WizBot with auto-update, just type `.die` in your Discord server. That's it!
* If you're running WizBot with **no** auto-update:
  * Kill your previous session.
    * Check the session name with `tmux ls`
    * Kill with `tmux kill-session -t WizBot` \(don't forget to replace "WizBot" with whatever you named your bot's session\).
  * Create a new session: `tmux new -s WizBot`
  * Run this command: `cd ~ && wget -N https://github.com/Wizkiller96/WizBot-BashScript/raw/1.9/linuxAIO.sh && bash linuxAIO.sh`
  * Choose Option `1` to download the most up to date version of WizBot.
  * Once it's done, choose Option `2` or `3` and detach the session by pressing **Ctrl+B**, release then **D**.

### Additional Information

* If you want to **see the active sessions**, run `tmux ls`. That will give you the list of the currently running sessions.
* If you want to **switch to/see a specific session**, type `tmux a -t WizBot` \(**WizBot** is the name of the session we created before so, replace **“WizBot”** with the session name you have created\).
  * If you want to go through the log, press **Ctrl+B**, release the keys then hit **Page Up** or **Page Down** to navigate.
  * Don't forget to always detach from the session by pressing **Ctrl+B** then **D** once you're done.
* If you want **create** a new session, run `tmux new -s WizBot`. If you want to **kill it**, run `tmux kill-session -t WizBot`

### Making WizBot persist upon system restarts \(tmux - For Advanced Users\)

This procedure is completely optional. We'll be using [_systemd_](https://en.wikipedia.org/wiki/Systemd) to handle WizBot during system shutdowns and reboots.

1. Start off by downloading the necessary scripts:
   * `cd ~ && wget https://raw.githubusercontent.com/Wizkiller96/WizBot-BashScript/1.9/wizbot.service`
   * `cd ~ && wget https://raw.githubusercontent.com/Wizkiller96/WizBot-BashScript/1.9/WizBotARN.sh`
   * `cd ~ && wget https://raw.githubusercontent.com/Wizkiller96/WizBot-BashScript/1.9/WizBotARU_Latest.sh`
2. If you **are** logged in as `root` and **don't want** WizBot to auto-update, ignore the procedures below and go straight to step 3.
   * Let's edit the script _systemd_ is going to use to start WizBot: `nano wizbot.service`
   * You should see the following:

     \`\`\`css

     \[Unit\]

     Description=WizBot 

\[Service\] WorkingDirectory=/root User=root Type=forking ExecStart=/usr/bin/tmux new-session -s WizBot -d '/bin/sh WizBotARN.sh' ExecStop=/bin/sleep 2

\[Install\] WantedBy=multi-user.target

\`\`\`

* Change `/root` from _"WorkingDirectory"_ to the directory that contains your WizBot folder.
  * For example, if your  bot is located in `/home/username/WizBot`, you should change `/root` to `/home/username`.
* Change `root` from _"User"_ to whatever username you're using.
* **Optional:** If you want WizBot to auto-update upon restarts, change `WizBotARN.sh` to `WizBotARU_Latest.sh`.
* Once you're done, press `Ctrl+X` to exit nano, type `y` to confirm the changes and `Enter` to go back to the terminal.
* Now the script needs to be moved to where _systemd_ stores their services. On Ubuntu, it's usually in `/etc/systemd/system`. If you are not using Ubuntu and are unsure about where _systemd_ stores stuff, [Google is your best friend](https://www.google.com/).
  * To do that, run this command: `sudo mv wizbot.service /etc/systemd/system/wizbot.service`
* Now it's time to reload _systemd_, so it loads our new script up: `sudo systemctl daemon-reload`
* Set the script to run upon system reboots: `sudo systemctl enable WizBot`
* Start WizBot on the current session: `sudo systemctl start WizBot`

And that's it. Every time your system restarts, _systemd_ should automatically startup your bot with tmux. If everything has gone well, you should be able to see WizBot on the list of processes being handled by tmux by running the `tmux ls` command.

### Managing WizBot on tmux with systemd

Here is a list of useful commands if you intend on managing Nadeko with _systemd_.

* `tmux ls` - lists all processes managed by tmux.
* `tmux a -t WizBot` - shows WizBot's log \(press `Ctrl+B` then `D` to exit\).
* `sudo systemctl start WizBot` - starts WizBot, if it has been stoped.
* `sudo systemctl restart WizBot` - restarts WizBot. Can be used while the bot is being run.
* `sudo systemctl stop WizBot` - completely shuts WizBot down.
* `sudo systemctl enable WizBot` - makes WizBot start automatically upon system reboots.
* `sudo systemctl disable WizBot` - stops WizBot from starting automatically upon system reboots. 
* `sudo systemctl status WizBot` - shows some information about your bot \(press `Ctrl+C` to exit\).

## Setting up WizBot on a Linux VPS \(Digital Ocean Droplet\)

If you want WizBot to play music for you 24/7 without having to hosting it on your PC and want to keep it cheap, reliable and convenient as possible, you can try WizBot on Linux Digital Ocean Droplet using the link [DigitalOcean](https://m.do.co/c/7290047d0c84) \(by using this link, you will get **$10 credit** and also support WizBot\)

**Setting up WizBot**  
Assuming you have followed the link above to setup an account and a Droplet with a 64-bit operational system on Digital Ocean and got the `IP address and root password (in your e-mail)` to login, it's time to get started.

**Go through this whole guide before setting up WizBot**

### Prerequisites

* Download [PuTTY](http://www.chiark.greenend.org.uk/~sgtatham/putty/download.html)
* Download [WinSCP](https://winscp.net/eng/download.php) _\(optional\)_
* [Create and invite the bot](http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/#creating-discord-bot-application).

### Starting up

* **Open PuTTY** and paste or enter your `IP address` and then click **Open**.

  If you entered your Droplets IP address correctly, it should show **login as:** in a newly opened window.

* Now for **login as:**, type `root` and press enter.
* It should then ask for a password. Type the `root password` you have received in your e-mail address, then press Enter.

If you are running your droplet for the first time, it will most likely ask you to change your root password. To do that, copy the **password you've received by e-mail** and paste it on PuTTY.

* To paste, just right-click the window \(it won't show any changes on the screen\), then press Enter.
* Type a **new password** somewhere, copy and paste it on PuTTY. Press Enter then paste it again.

  **Save the new password somewhere safe.**

After that, your droplet should be ready for use. [Follow the guide from the beginning](https://wizbot.readthedocs.io/en/latest/guides/Linux%20Guide/#getting-started) to set WizBot up on your newly created VPS.

#### Setting up WinSCP

WinSCP is useful for transfering files between a local system \(your computer\) and a remote system \(your VPS\). To set it up:

* Open **WinSCP**
* Click on **New Site** \(top-left corner\).
* On the right-hand side, you should see **File Protocol** above a drop-down selection menu.
* Select **SFTP** _\(SSH File Transfer Protocol\)_ if its not already selected.
* Now, in **Host name:** paste or type in your `Digital Ocean Droplets IP address` and leave `Port: 22` \(no need to change it\).
* In **Username:** type `root`
* In **Password:** type `the new root password (you changed at the start)`
* Click on **Login**, it should connect.
* If everything goes well, you should see the WizBot folder which was created by Git earlier on the right-hand side window. You should now be able to download and upload files to your VPS.

