## Migration from 2.x 

##### ⚠ If you're already hosting NadekoBot, _You **MUST** update to latest version of 2.x and **run your bot at least once**_ before switching over to v3.

#### [Linux migration instructions](../migration-guide/#linux)

## Linux From Source 

Open Terminal (if you're on an installation with a window manager) and navigate to the location where you want to install the bot (for example `cd ~`) 

##### Installation Instructions

1. Download and run the **new** installer script `cd ~ && wget -N https://gitlab.com/Kwoth/nadeko-bash-installer/-/raw/master/linuxAIO.sh && bash linuxAIO.sh`
2. Install prerequisites (type `1` and press enter)
3. Download the bot (type `2` and press enter)
4. Exit the installer in order to set up your `creds.yml` 
5. Copy the creds.yml template `cp nadekobot/output/creds_example.yml nadekobot/output/creds.yml` 
6. Open `nadekobot/output/creds.yml` with your favorite text editor. We will use nano here
    - `nano nadekobot/output/creds.yml`
7. [Enter your bot's token](../../creds-guide)
    - After you're done, you can close nano (and save the file) by inputting, in order 
       - `CTRL` + `X`
       - `Y`
       - `Enter`
8. Run the bot (type `3` and press enter)

##### Update Instructions

1. ⚠ Stop the bot
2. Update and run the **new** installer script `cd ~ && wget -N https://gitlab.com/Kwoth/nadeko-bash-installer/-/raw/master/linuxAIO.sh && bash linuxAIO.sh`
3. Update the bot (type `2` and press enter)
4. Run the bot (type `3` and press enter)
5. 🎉 

## Linux Release

##### Installation Instructions

1. Download the latest release from <https://gitlab.com/Kwoth/nadekobot/-/releases>
    - Look for the file called "X.XX.X-linux-x64-build.tar" (where X.XX.X is a series of numbers) and download it
2. Untar it 
    - ⚠ Make sure that you change X.XX.X to the same series of numbers as in step 1!
    - `tar xf X.XX.X-linux-x64-build.tar`
3. Rename the `nadekobot-linux-x64` to `nadekobot` 
    - `mv nadekobot-linux-x64 nadekobot`
4. Move into nadekobot directory and make NadekoBot executable
    - `cd nadekobot && chmod +x NadekoBot`
5. Copy the creds.yml template 
    - `cp creds_example.yml creds.yml` 
6. Open `creds.yml` with your favorite text editor. We will use nano here
    - `nano nadekobot/output/creds.yml`
8. [Enter your bot's token](#creds-guide)
    - After you're done, you can close nano (and save the file) by inputting, in order 
       - `CTRL` + `X`
       - `Y`
       - `Enter`
9. Run the bot
    - `./NadekoBot`

##### Update Instructions

1. Stop the bot
2. Download the latest release from <https://gitlab.com/Kwoth/nadekobot/-/releases>
    - Look for the file called "x.x.x-linux-x64-build.tar" (where `X.X.X` is a version, for example 3.0.4) and download it
3. Untar it 
    - ⚠ Make sure that you change `X.X.X` to the same series of numbers as in step 2!
    - `tar xf x.x.x-linux-x64-build.tar`
4. Rename the old nadekobot directory to nadekobot-old (remove your old backup first if you have one, or back it up under a different name)
    - `rm -rf nadekobot-old 2>/dev/null`
    - `mv nadekobot nadekobot-old`
5. Rename the new nadekobot directory to nadekobot
    - `mv nadekobot-linux-x64 nadekobot`
6. Remove old strings and aliases to avoid overwriting the updated versions of those files  
    - ⚠ If you've modified said files, back them up instead
    - `rm nadekobot-old/data/aliases.yml`
    - `rm -r nadekobot-old/data/strings`
7. Copy old data
    - `cp -RT nadekobot-old/data/ nadekobot/data`
8. Copy creds.yml
    - `cp nadekobot-old/creds.yml nadekobot/`
9. Move into nadekobot directory and make the NadekoBot executable
    - `cd nadekobot && chmod +x NadekoBot`
10. Run the bot 
    - `./NadekoBot`

🎉 Enjoy

##### Steps 3 - 9 as a single command  

Don't forget to change X.XX.X to match step 2.
```sh
tar xf X.XX.X-linux-x64-build.tar && \
rm -rf nadekobot-old 2>/dev/null && \
mv nadekobot nadekobot-old && \
mv nadekobot-linux-x64 nadekobot && \
rm nadekobot-old/data/aliases.yml && \
rm -r nadekobot-old/data/strings && \
cp -RT nadekobot-old/data/ nadekobot/data && \
cp nadekobot-old/creds.yml nadekobot/ && \
cd nadekobot && chmod +x NadekoBot
```

## Running Nadeko

While there are two run modes built into the installer, these options only run Nadeko within the current session. Below are 3 methods of running Nadeko as a background process.

### Tmux (Preferred Method)

Using `tmux` is the simplest method, and is therefore recommended for most users.

1. Start a tmux session:
    - `tmux`
2. Navigate to the project's root directory
    - Project root directory location example: `/home/user/nadekobot/`
3. Enter the `output` directory:
    - `cd output`
4. Run the bot using:
    - `dotnet NadekoBot.dll`
5. Detatch the tmux session:
    - Press `Ctrl` + `B`
    - Then press `D`

Nadeko should now be running in the background of your system. To re-open the tmux session to either update, restart, or whatever, execute `tmux a`.

### Systemd

Compared to using tmux, this method requires a little bit more work to set up, but has the benefit of allowing Nadeko to automatically start back up after a system reboot or the execution of the `.die` command.

1. Navigate to the project's root directory
    - Project root directory location example: `/home/user/nadekobot/`
2. Use the following command to create a service that will be used to start Nadeko:

    ```bash
    echo "[Unit]
    Description=NadekoBot service
    After=network.target
    StartLimitIntervalSec=60
    StartLimitBurst=2
    
    [Service]
    Type=simple
    User=$USER
    WorkingDirectory=$PWD/output
    # If you want Nadeko to be compiled prior to every startup, uncomment the lines
    # below. Note  that it's not neccessary unless you are personally modifying the
    # source code.
    #ExecStartPre=/usr/bin/dotnet build ../src/NadekoBot/NadekoBot.csproj -c Release -o output/
    ExecStart=/usr/bin/dotnet NadekoBot.dll
    Restart=on-failure
    RestartSec=5
    StandardOutput=syslog
    StandardError=syslog
    SyslogIdentifier=NadekoBot
    
    [Install]
    WantedBy=multi-user.target" | sudo tee /etc/systemd/system/nadeko.service
    ```
    
3. Make the new service available:
    - `sudo systemctl daemon-reload`
4. Start Nadeko:
    - `sudo systemctl start nadeko.service && sudo systemctl enable nadeko.service`
    

### Systemd + Script

This method is similar to the one above, but requires one extra step, with the added benefit of better error logging and control over what happens before and after the startup of Nadeko.

1. Locate the project and move to its parent directory
    - Project location example: `/home/user/nadekobot/`
    - Parent directory example: `/home/user/`
2. Use the following command to create a service that will be used to execute `NadekoRun.sh`:

    ```bash
    echo "[Unit]
    Description=NadekoBot service
    After=network.target
    StartLimitIntervalSec=60
    StartLimitBurst=2
    
    [Service]
    Type=simple
    User=$USER
    WorkingDirectory=$_WORKING_DIR
    ExecStart=/bin/bash NadekoRun.sh
    Restart=on-failure
    RestartSec=5
    StandardOutput=syslog
    StandardError=syslog
    SyslogIdentifier=NadekoBot
    
    [Install]
    WantedBy=multi-user.target" | sudo tee /etc/systemd/system/nadeko.service
    ```
    
3. Make the new service available:
    - `sudo systemctl daemon-reload`
4. Use the following command to create a script that will be used to start Nadeko:
    
    ```bash
    {
    echo '#!/bin/bash'
    echo ""
    echo "echo \"Running NadekoBot in the background with auto restart\"
    youtube-dl -U
    
    # If you want Nadeko to be compiled prior to every startup, uncomment the lines
    # below. Note  that it's not necessary unless you are personally modifying the
    # source code.
    #echo \"Compiling NadekoBot...\"
    #cd \"$PWD\"/nadekobot
    #dotnet build src/NadekoBot/NadekoBot.csproj -c Release -o output/

    echo \"Starting NadekoBot...\"
    
    while true; do
        if [[ -d $PWD/nadekobot/output ]]; then
            cd $PWD/nadekobot/output || {
                echo \"Failed to change working directory to $PWD/nadekobot/output\" >&2
                echo \"Ensure that the working directory inside of '/etc/systemd/system/nadeko.service' is correct\"
                echo \"Exiting...\"
                exit 1
            }
        else
            echo \"$PWD/nadekobot/output doesn't exist\"
            exit 1
        fi
        
        dotnet NadekoBot.dll || {
            echo \"An error occurred when trying to start NadekBot\"
            echo \"Exiting...\"
            exit 1
        }
        
        echo \"Waiting for 5 seconds...\"
        sleep 5
        youtube-dl -U
        echo \"Restarting NadekoBot...\"
    done
    
    echo \"Stopping NadekoBot...\""
    } > NadekoRun.sh
    ```
    
5. Start Nadeko:
    - `sudo systemctl start nadeko.service && sudo systemctl enable nadeko.service`
