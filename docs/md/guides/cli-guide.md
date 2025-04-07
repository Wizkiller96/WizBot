# WizBot CLI Guide (via Bash Installer)

### Supported Operating Systems

--8<-- "md/snippets/supported-platforms.md:linux"
--8<-- "md/snippets/supported-platforms.md:macos"

### Prerequisites

macOS:

- [Homebrew](https://brew.sh/)
- [Curl](#__tabbed_1_5)

Linux:

- [Curl](#__tabbed_1_1)

---

??? note "24/7 Up-time via VPS (Digital Ocean Guide)"
    --8<-- "md/guides/vps-linux-guide.md"

??? note "Creating a Discord Bot & Getting Credentials"
    --8<-- "md/creds-guide.md"

---

## Installation Instructions

Open Terminal (if you're on an installation with a window manager) and navigate to the location where you want to install the bot (for example `cd ~`)

1. First make sure that curl is installed

    /// tab | Ubuntu | Debian | Mint

    ```bash
    sudo apt install curl
    ```

    ///
    /// tab | Rocky | Alma | Fedora

    ```bash
    sudo dnf install curl
    ```

    ///
    /// tab | openSUSE

    ```bash
    sudo zypper install curl
    ```

    ///
    /// tab | Arch | Artix

    ```bash
    sudo pacman -S curl
    ```

    ///
    /// tab | macOS

    ```bash
    brew install curl
    ```

    ///

2. Download and run the **new** installer script
    ``` sh
    cd ~
    curl -L -o n-install.sh https://raw.githubusercontent.com/Wizkiller96/bash-installer/refs/heads/v6/w-install.sh
    bash w-install.sh
    ```
3. Install the bot (type `1` and press enter)
4. Edit creds (type `3` and press enter)
    - *ALTERNATIVELY*, you can exit the installer (option `6`) and edit `wizbot/creds.yml` file yourself
5. Follow the instruction [below](#creating-your-own-discord-bot) to create your own Discord bot and obtain the credentials needed to run it.
    - After you're done, you can close nano (and save the file) by inputting, in order:
        - `CTRL` + `X`
        - `Y`
        - `Enter`
6. Run the installer script again
    - `bash w-install.sh`
7. Run the bot (type `3` and press enter)
8. Done!

## Update Instructions

1. ⚠ Stop the bot ⚠
2. Navigate to your bot's folder, we'll use home directory as an example
    - `cd ~`
3. Simply re-install the bot with a newer version by running the installer script
    - `curl -L -o w-install.sh https://raw.githubusercontent.com/Wizkiller96/bash-installer/refs/heads/v6/w-install.sh && bash w-install.sh`
4. Select option 1, and select a NEWER version

## Running WizBot

There are two main methods to run WizBot: using `tmux` (macOS and Linux) or using `systemd` with a script (Linux only).

/// tab | Tmux (Preferred Method)

Using `tmux` is the simplest method, and is therefore recommended for most users.

!!! warning
    Before proceeding, make sure your bot is not currently running by either running `.die` in your Discord server or exiting the process with `Ctrl+C`.

1. Access the directory where `w-install.sh` and `wizbot` is located.
2. Create a new tmux session: `tmux new -s wizbot`
    - The above command will create a new session named **wizbot**. You may replace **wizbot** with any name you prefer.
3. Run the installer: `bash w-install.sh`
4. Start the bot by typing `3` and pressing `Enter`.
5. Detach from the tmux session, allowing the bot to run in the background:
    - Press `Ctrl` + `B`
    - Then press `D`

Now check your Discord server, the bot should be online. WizBot should now be running in the background of your system.

To re-open the tmux session to either update, restart, or whatever, execute `tmux a -t wizbot`. *(Make sure to replace "wizbot" with your session name. If you didn't change it, leave it as it is.)*

///
/// tab | Systemd

!!! note
    Systemd is only available on Linux. macOS utilizes Launchd, which is not covered in this guide. If you're on macOS, please use the `tmux` method, or use [WizBot Updater](desktop-guide.md) to run WizBot.

This method is a bit more complex and involved, but comes with the added benefit of better error logging and control over what happens before and after the startup of WizBot.

1. Access the directory where `w-install.sh` and `wizbot` is located.
2. Use the following command to create a service that will be used to execute `WizBotRun.bash`:
    ```bash
    echo "[Unit]
    Description=WizBot service
    After=network.target
    StartLimitIntervalSec=60
    StartLimitBurst=2

    [Service]
    Type=simple
    User=$USER
    WorkingDirectory=$PWD
    ExecStart=/bin/bash WizBotRun.bash
    #ExecStart=./wizbot/WizBot
    Restart=on-failure
    RestartSec=5
    StandardOutput=journal
    StandardError=journal
    SyslogIdentifier=WizBot

    [Install]
    WantedBy=multi-user.target" | sudo tee /etc/systemd/system/wizbot.service
    ```
3. Make the new service available: `sudo systemctl daemon-reload`
4. Use the following command to create a script that will be used to start WizBot:
    ```bash
    cat <<EOF > WizBotRun.bash
    #!/bin/bash

    export PATH="$HOME/.local/bin:$PATH"

    is_python3_installed=\$(command -v python3 &>/dev/null && echo true || echo false)
    is_yt_dlp_installed=\$(command -v yt-dlp &>/dev/null && echo true || echo false)

    [[ \$is_python3_installed == true ]] \\
        && echo "[INFO] python3 path: \$(which python3)" \\
        && echo "[INFO] python3 version: \$(python3 --version)"
    [[ \$is_yt_dlp_installed == true ]] \\
        && echo "[INFO] yt-dlp path: \$(which yt-dlp)"

    echo "[INFO] Running WizBot in the background with auto restart"
    if [[ \$is_yt_dlp_installed == true ]]; then
        yt-dlp -U || echo "[ERROR] Failed to update 'yt-dlp'" >&2
    fi

    echo "[INFO] Starting WizBot..."

    while true; do
        if [[ -d $PWD/wizbot ]]; then
            cd "$PWD/wizbot" || {
                echo "[ERROR] Failed to change working directory to '$PWD/wizbot'" >&2
                echo "[INFO] Exiting..."
                exit 1
            }
        else
            echo "[WARN] '$PWD/wizbot' doesn't exist" >&2
            echo "[INFO] Exiting..."
            exit 1
        fi

        ./WizBot || {
            echo "[ERROR] An error occurred when trying to start WizBot" >&2
            echo "[INFO] Exiting..."
            exit 1
        }

        echo "[INFO] Waiting 5 seconds..."
        sleep 5
        if [[ \$is_yt_dlp_installed == true ]]; then
            yt-dlp -U || echo "[ERROR] Failed to update 'yt-dlp'" >&2
        fi
        echo "[INFO] Restarting WizBot..."
    done

    echo "[INFO] Stopping WizBot..."
    EOF
    ```

With everything set up, you can run WizBot in one of three modes:

1. **Auto-Restart Mode**: WizBot will restart automatically if you restart it via the `.die` command.
    - To enable this mode, start the service: `sudo systemctl start wizbot`
2. **Auto-Restart on Reboot Mode**: In addition to auto-restarting after `.die`, WizBot will also start automatically on system reboot.
    - To enable this mode, run:
        ```bash
        sudo systemctl enable wizbot
        sudo systemctl start wizbot
        ```
3. **Standard Mode**: WizBot will stop completely when you use `.die`, without restarting automatically.
    - To switch to this mode:
        1. Stop the service: `sudo systemctl stop wizbot`
        2. Edit the service file: `sudo <editor> /etc/systemd/system/wizbot.service`
        3. Modify the `ExecStart` line:
            - **Comment out**: `ExecStart=/bin/bash WizBotRun.bash`
            - **Uncomment**: `#ExecStart=./wizbot/WizBot`
        4. Save and exit the editor.
        5. Reload systemd: `sudo systemctl daemon-reload`
        6. Disable automatic startup: `sudo systemctl disable wizbot`
        7. Start WizBot manually: `sudo systemctl start wizbot`

///
