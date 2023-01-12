# Setting up WizBot on Linux

| Table of Contents                            |
|:---------------------------------------------|
| [Linux From Source]                          |
| [Source Update Instructions]                 |
| [Linux Release]                              |
| [Release Update Instructions]                |
| [Tmux (Preferred Method)]                    |
| [Systemd]                                    |
| [Systemd + Script]                           |
| [Setting up WizBot on a VPS (Digital Ocean)] |

#### Operating System Compatibility

It is recommended that you use **Ubuntu 20.04**, as there have been nearly no problems with it. Also, **32-bit systems are incompatible**.

### Ubuntu 22.04 is ruled as incompatible so double check which ubuntu version you are using.

##### Compatible operating systems:

- Ubuntu: 16.04, 18.04, 20.04, 21.04, 21.10
- Mint: 19, 20
- Debian: 10, 11
- CentOS: 7
- openSUSE
- Fedora: 33, 34, 35

## Linux From Source 

##### Migration from v3 -> v4

Follow the following few steps only if you're migrating from v3. If not, skip to installation instructions.

Use the new installer script:  `cd ~ && wget -N https://github.com/Wizkiller96/wizbot-bash-installer/-/raw/v4/linuxAIO.sh && bash linuxAIO.sh`
> - Install prerequisites (type `1` and press `enter`)
> - Download (type `2` and press `enter`)
> - Run (type `3` and press `enter`)
> - Done

##### Installation Instructions

Open Terminal (if you're on an installation with a window manager) and navigate to the location where you want to install the bot (for example `cd ~`) 

1. Download and run the **new** installer script `cd ~ && wget -N https://github.com/Wizkiller96/wizbot-bash-installer/raw/v4/linuxAIO.sh && bash linuxAIO.sh`
2. Install prerequisites (type `1` and press enter)
3. Download the bot (type `2` and press enter)
4. Exit the installer (type `6` and press enter)
5. Copy the creds.yml template `cp wizbot/output/creds_example.yml wizbot/output/creds.yml` 
6. Open `wizbot/output/creds.yml` with your favorite text editor. We will use nano here
    - `nano wizbot/output/creds.yml`
7. [Click here to follow creds guide](../../creds-guide)
    - After you're done, you can close nano (and save the file) by inputting, in order 
       - `CTRL` + `X`
       - `Y`
       - `Enter`
8. Run the installer script again `cd ~ && wget -N https://github.com/Wizkiller96/wizbot-bash-installer/raw/v4/linuxAIO.sh && bash linuxAIO.sh`
9. Run the bot (type `3` and press enter)

##### Source Update Instructions

1. ⚠ Stop the bot ⚠
2. Update and run the **new** installer script `cd ~ && wget -N https://github.com/Wizkiller96/wizbot-bash-installer/raw/v4/linuxAIO.sh && bash linuxAIO.sh`
3. Update the bot (type `2` and press enter)
4. Run the bot (type `3` and press enter)
5. 🎉 

## **⚠ IF YOU ARE FOLLOWING THE GUIDE ABOVE, IGNORE THIS SECTION ⚠**

## Linux Release

###### Prerequisites

1. (Optional) Installing Redis
   - ubuntu installation command: `sudo apt-get install redis-server`
2. Playing music requires `ffmpeg`, `libopus`, `libsodium` and `youtube-dl` (which in turn requires python3)
   - ubuntu installation command: `sudo apt-get install ffmpeg libopus0 opus-tools libopus-dev libsodium-dev -y`
3. Make sure your python is version 3+ with `python --version`
   - if it's not, you can install python 3 and make it the default with: `sudo apt-get install python3.8 python-is-python3`

*You can use wizbot bash script [prerequisites installer](https://github.com/Wizkiller96/wizbot-bash-installer/blob/v4/w-prereq.sh) as a reference*

##### Installation Instructions

1. Download the latest release from <https://gitlab.com/WizNet/wizbot/-/releases>
    - Look for the file called "X.XX.X-linux-x64-build.tar" (where X.XX.X is a series of numbers) and download it
2. Untar it 
    - ⚠ Make sure that you change X.XX.X to the same series of numbers as in step 1!
    - `tar xf X.XX.X-linux-x64-build.tar`
3. Rename the `wizbot-linux-x64` to `wizbot` 
    - `mv wizbot-linux-x64 wizbot`
4. Move into wizbot directory and make WizBot executable
    - `cd wizbot && chmod +x WizBot`
5. Copy the creds.yml template 
    - `cp creds_example.yml creds.yml` 
6. Open `creds.yml` with your favorite text editor. We will use nano here
    - `nano wizbot/output/creds.yml`
8. [Click here to follow creds guide](../../creds-guide)
    - After you're done, you can close nano (and save the file) by inputting, in order 
       - `CTRL` + `X`
       - `Y`
       - `Enter`
9. Run the bot
    - `./WizBot`

##### Release Update Instructions

1. Stop the bot
2. Download the latest release from <https://gitlab.com/WizNet/WizBot/-/releases>
    - Look for the file called "x.x.x-linux-x64-build.tar" (where `X.X.X` is a version, for example 3.0.4) and download it
3. Untar it 
    - ⚠ Make sure that you change `X.X.X` to the same series of numbers as in step 2!
    - `tar xf x.x.x-linux-x64-build.tar`
4. Rename the old wizbot directory to wizbot-old (remove your old backup first if you have one, or back it up under a different name)
    - `rm -rf wizbot-old 2>/dev/null`
    - `mv wizbot wizbot-old`
5. Rename the new wizbot directory to wizbot
    - `mv wizbot-linux-x64 wizbot`
6. Remove old strings and aliases to avoid overwriting the updated versions of those files  
    - ⚠ If you've modified said files, back them up instead
    - `rm wizbot-old/data/aliases.yml`
    - `rm -r wizbot-old/data/strings`
7. Copy old data
    - `cp -RT wizbot-old/data/ wizbot/data`
8. Copy creds.yml
    - `cp wizbot-old/creds.yml wizbot/`
9. Move into wizbot directory and make the WizBot executable
    - `cd wizbot && chmod +x WizBot`
10. Run the bot 
    - `./WizBot`

🎉 Enjoy

##### Steps 3 - 9 as a single command  

Don't forget to change X.XX.X to match step 2.
```sh
tar xf X.XX.X-linux-x64-build.tar && \
rm -rf wizbot-old 2>/dev/null && \
mv wizbot wizbot-old && \
mv wizbot-linux-x64 wizbot && \
rm wizbot-old/data/aliases.yml && \
rm -r wizbot-old/data/strings && \
cp -RT wizbot-old/data/ wizbot/data && \
cp wizbot-old/creds.yml wizbot/ && \
cd wizbot && chmod +x WizBot
```

## Running WizBot

While there are two run modes built into the installer, these options only run WizBot within the current session. Below are 3 methods of running WizBot as a background process.

### Tmux Method (Preferred)

Using `tmux` is the simplest method, and is therefore recommended for most users.

1. Start a tmux session:
    - `tmux`
2. Run the installer: `bash linuxAIO.sh`

3. There are a few options when it comes to running WizBot.

   - Run `3` to *Run the bot normally*
   - Run `4` to *Run the bot with Auto Restart* (This is may or may not work)

4. If option `4` was selected, you have the following options
```
1. Run Auto Restart normally without updating WizBot.
2. Run Auto Restart and update WizBot.
3. Exit

Choose:
[1] to Run WizBot with Auto Restart on "die" command without updating.
[2] to Run with Auto Updating on restart after using "die" command.
```
- Run `1` to update the bot upon restart. (This is done using the `.die` command)
- Run `2` to restart the bot without updating. (This is also done using the `.die` command)

5. That's it! to detatch the tmux session:
    - Press `Ctrl` + `B`
    - Then press `D`

WizBot should now be running in the background of your system. To re-open the tmux session to either update, restart, or whatever, execute `tmux a`.

### Systemd

Compared to using tmux, this method requires a little bit more work to set up, but has the benefit of allowing WizBot to automatically start back up after a system reboot or the execution of the `.die` command.

1. Navigate to the project's root directory
    - Project root directory location example: `/home/user/wizbot/`
2. Use the following command to create a service that will be used to start WizBot:

    ```bash
    echo "[Unit]
    Description=WizBot service
    After=network.target
    StartLimitIntervalSec=60
    StartLimitBurst=2
    
    [Service]
    Type=simple
    User=$USER
    WorkingDirectory=$PWD/output
    # If you want WizBot to be compiled prior to every startup, uncomment the lines
    # below. Note  that it's not neccessary unless you are personally modifying the
    # source code.
    #ExecStartPre=/usr/bin/dotnet build ../src/WizBot/WizBot.csproj -c Release -o output/
    ExecStart=/usr/bin/dotnet WizBot.dll
    Restart=on-failure
    RestartSec=5
    StandardOutput=syslog
    StandardError=syslog
    SyslogIdentifier=WizBot
    
    [Install]
    WantedBy=multi-user.target" | sudo tee /etc/systemd/system/wizbot.service
    ```
    
3. Make the new service available:
    - `sudo systemctl daemon-reload`
4. Start WizBot:
    - `sudo systemctl start wizbot.service && sudo systemctl enable wizbot.service`
    

### Systemd + Script

This method is similar to the one above, but requires one extra step, with the added benefit of better error logging and control over what happens before and after the startup of WizBot.

1. Locate the project and move to its parent directory
    - Project location example: `/home/user/wizbot/`
    - Parent directory example: `/home/user/`
2. Use the following command to create a service that will be used to execute `WizBotRun.sh`:

    ```bash
    echo "[Unit]
    Description=WizBot service
    After=network.target
    StartLimitIntervalSec=60
    StartLimitBurst=2
    
    [Service]
    Type=simple
    User=$USER
    WorkingDirectory=$_WORKING_DIR
    ExecStart=/bin/bash WizBotRun.sh
    Restart=on-failure
    RestartSec=5
    StandardOutput=syslog
    StandardError=syslog
    SyslogIdentifier=WizBot
    
    [Install]
    WantedBy=multi-user.target" | sudo tee /etc/systemd/system/wizbot.service
    ```
    
3. Make the new service available:
    - `sudo systemctl daemon-reload`
4. Use the following command to create a script that will be used to start WizBot:
    
    ```bash
    {
    echo '#!/bin/bash'
    echo ""
    echo "echo \"Running WizBot in the background with auto restart\"
    youtube-dl -U
    
    # If you want WizBot to be compiled prior to every startup, uncomment the lines
    # below. Note  that it's not necessary unless you are personally modifying the
    # source code.
    #echo \"Compiling WizBot...\"
    #cd \"$PWD\"/wizbot
    #dotnet build src/WizBot/WizBot.csproj -c Release -o output/

    echo \"Starting WizBot...\"
    
    while true; do
        if [[ -d $PWD/wizbot/output ]]; then
            cd $PWD/wizbot/output || {
                echo \"Failed to change working directory to $PWD/wizbot/output\" >&2
                echo \"Ensure that the working directory inside of '/etc/systemd/system/wizbot.service' is correct\"
                echo \"Exiting...\"
                exit 1
            }
        else
            echo \"$PWD/wizbot/output doesn't exist\"
            exit 1
        fi
        
        dotnet WizBot.dll || {
            echo \"An error occurred when trying to start NadekBot\"
            echo \"Exiting...\"
            exit 1
        }
        
        echo \"Waiting for 5 seconds...\"
        sleep 5
        youtube-dl -U
        echo \"Restarting WizBot...\"
    done
    
    echo \"Stopping WizBot...\""
    } > WizBotRun.sh
    ```
    
5. Start WizBot:
    - `sudo systemctl start wizbot.service && sudo systemctl enable wizbot.service`

### Setting up WizBot on a Linux VPS (Digital Ocean Droplet)

If you want WizBot to play music for you 24/7 without having to hosting it on your PC and want to keep it cheap, reliable and convenient as possible, you can try WizBot on Linux Digital Ocean Droplet using the link [DigitalOcean](https://m.do.co/c/7290047d0c84) (by using this link, you will get **$10 credit** and also support WizBot)

To set up the VPS, please select the options below
```
These are the min requirements you must follow:

OS: Any between Ubuntu, Fedora, and Debian

Plan: Basic

CPU options: regular with SSD
1 GB / 1 CPU
25 GB SSD Disk
1000 GB transfer

Note: You can select the cheapest option with 512 MB /1 CPU but this has been a hit or miss.

Datacenter region: Choose one depending on where you are located.

Authentication: Password or SSH 
(Select SSH if you know what you are doing, otherwise choose password)
```

**Setting up WizBot**
Assuming you have followed the link above to setup an account and a Droplet with a 64-bit operational system on Digital Ocean and got the `IP address and root password (in your e-mail)` to login, it's time to get started.

**This section is only relevant to those who want to host WizBot on DigitalOcean. Go through this whole section before setting the bot up.**

#### Prerequisites

- Download [PuTTY](http://www.chiark.greenend.org.uk/~sgtatham/putty/download.html)
- Download [WinSCP](https://winscp.net/eng/download.php) *(optional)*
- [Create and invite the bot](../../creds-guide).

#### Starting up

- **Open PuTTY** and paste or enter your `IP address` and then click **Open**.  
  If you entered your Droplets IP address correctly, it should show **login as:** in a newly opened window.
- Now for **login as:**, type `root` and press enter.
- It should then ask for a password. Type the `root password` you have received in your e-mail address, then press Enter.

If you are running your droplet for the first time, it will most likely ask you to change your root password. To do that, copy the **password you've received by e-mail** and paste it on PuTTY.

- To paste, just right-click the window (it won't show any changes on the screen), then press Enter.
- Type a **new password** somewhere, copy and paste it on PuTTY. Press Enter then paste it again.

**Save the new password somewhere safe.**

After that, your droplet should be ready for use. [Follow the guide from the beginning](#linux-from-source) to set WizBot up on your newly created VPS.

[Linux From Source]: #linux-from-source
[Source Update Instructions]: #source-update-instructions
[Linux Release]: #linux-release
[Release Update Instructions]: #release-update-instructions
[Tmux (Preferred Method)]: #tmux-preferred-method
[Systemd]: #systemd
[Systemd + Script]: #systemd-script
[Setting up WizBot on a VPS (Digital Ocean)]: #setting-up-wizbot-on-a-linux-vps-digital-ocean-droplet
