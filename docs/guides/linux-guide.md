## Migration from 2.x 

##### ⚠ If you're already hosting WizBot, _You **MUST** update to latest version of 2.x and **run your bot at least once**_ before switching over to v3.

#### [Linux migration instructions](../migration-guide/#linux)

## Linux From Source 

Open Terminal (if you're on an installation with a window manager) and navigate to the location where you want to install the bot (for example `cd ~`) 

##### Installation Instructions

1. Download and run the **new** installer script `cd ~ && wget -N https://github.com/Wizkiller96/wizbot-bash-installer/-/raw/master/linuxAIO.sh && bash linuxAIO.sh`
2. Install prerequisites (type `1` and press enter)
3. Download the bot (type `2` and press enter)
4. Exit the installer in order to set up your `creds.yml` 
5. Copy the creds.yml template `cp wizbot/output/creds_example.yml wizbot/output/creds.yml` 
6. Open `wizbot/output/creds.yml` with your favorite text editor. We will use nano here
    - `nano wizbot/output/creds.yml`
7. [Enter your bot's token](../../creds-guide)
    - After you're done, you can close nano (and save the file) by inputting, in order 
       - `CTRL` + `X`
       - `Y`
       - `Enter`
8. Run the bot (type `3` and press enter)

##### Update Instructions

1. ⚠ Stop the bot
2. Update and run the **new** installer script `cd ~ && wget -N https://github.com/Wizkiller96/wizbot-bash-installer/-/raw/master/linuxAIO.sh && bash linuxAIO.sh`
3. Update the bot (type `2` and press enter)
4. Run the bot (type `3` and press enter)
5. 🎉 

## Linux Release

##### Installation Instructions

1. Download the latest release from <https://github.com/Wizkiller96/WizBot/-/releases>
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
8. [Enter your bot's token](#creds-guide)
    - After you're done, you can close nano (and save the file) by inputting, in order 
       - `CTRL` + `X`
       - `Y`
       - `Enter`
9. Run the bot
    - `./WizBot`

##### Update Instructions

1. Stop the bot
2. Download the latest release from <https://github.com/Wizkiller96/WizBot/-/releases>
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

### Tmux (Preferred Method)

Using `tmux` is the simplest method, and is therefore recommended for most users.

1. Start a tmux session:
    - `tmux`
2. Navigate to the project's root directory
    - Project root directory location example: `/home/user/wizbot/`
3. Enter the `output` directory:
    - `cd output`
4. Run the bot using:
    - `dotnet WizBot.dll`
5. Detatch the tmux session:
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
    
    [Service]
    Type=simple
    User=$USER
    WorkingDirectory=$PWD/output
    # If you want WizBot to be compiled prior to every startup, uncomment the lines
    # below. Note  that it's not neccessary unless you are personally modifying the
    # source code.
    #ExecStartPre=/usr/bin/dotnet build ../src/WizBot/WizBot.csproj -c Release -o output/
    ExecStart=/usr/bin/dotnet WizBot.dll
    StandardOutput=syslog
    StandardError=syslog
    SyslogIdentifier=WizBot
    Restart=always
    
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
    
    [Service]
    Type=simple
    User=$USER
    WorkingDirectory=$PWD
    ExecStart=/bin/bash WizBotRun.sh
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
    echo "#\!/bin/bash
    
    echo \"\"
    echo \"Running WizBot in the background with auto restart\"
    youtube-dl -U
    
    # If you want WizBot to be compiled prior to every startup, uncomment the lines
    # below. Note  that it's not neccessary unless you are personally modifying the
    # source code.
    #echo \"Compiling WizBot...\"
    #cd \"$PWD\"/wizbot
    #dotnet build src/WizBot/WizBot.csproj -c Release -o output/

    echo \"Starting WizBot...\"
    
    while true; do
        {
            cd \"$PWD\"/wizbot/output
            dotnet WizBot.dll
        ## If a non-zero exit code is produced, exit this script.
        } || {
            error_code=\"\$?\"
            echo \"An error occurred when trying to start WizBot\"
            echo \"EXIT CODE: \$?\"
            exit \"\$error_code\"
        }
    
        youtube-dl -U
        echo \"Restarting WizBot...\"
    done
    
    echo \"Stopping WizBot...\"" > WizBotRun.sh
    ```
    
5. Start WizBot:
    - `sudo systemctl start wizbot.service && sudo systemctl enable wizbot.service`