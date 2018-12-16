# Setting Up WizBot on OSX \(macOS\)

## Prerequisites

* macOS 10.12 \(Sierra\) or higher \(needed for .NET Core 2.x\).
* [Homebrew](http://brew.sh/). Install it with `/usr/bin/ruby -e "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install)"` or update it with `brew update`.
* Text Editor \(TextWrangler, or equivalent\) or outside editor such as [Atom](https://atom.io/).
* [Create Discord Bot application](http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/#creating-discord-bot-application) and [Invite the bot to your server](http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/#inviting-your-bot-to-your-server). 

**Installing dependencies with Homebrew**

In terminal:

```text
brew install wget
brew install git
brew install ffmpeg
brew update && brew upgrade ffmpeg
brew install openssl
brew install opus
brew install opus-tools
brew install opusfile
brew install libffi
brew install libsodium
brew install tmux
brew install python
brew install youtube-dl
brew install redis
brew services start redis
```

**Installing .NET Core SDK**

* `ln -s /usr/local/opt/openssl/lib/libcrypto.1.0.0.dylib /usr/local/lib/`
* `ln -s /usr/local/opt/openssl/lib/libssl.1.0.0.dylib /usr/local/lib/`
* Download the [.NET Core SDK](https://www.microsoft.com/net/core#macos)
* Open the `.pkg` file you downloaded and install it.
* `ln -s /usr/local/share/dotnet/dotnet /usr/local/bin`

**Installing xcode-select**

* `xcode-select --install`

A dialog box will open asking if you want to install `xcode-select`. Select install and finish the installation.

## Getting WizBot Ready to Run

`cd ~ && wget -N https://github.com/Wizkiller96/WizBot-BashScript/raw/1.9/linuxAIO.sh && bash linuxAIO.sh` Choose `1. Download WizBot` Once Installation is completed you should see the options again.

Choose Option 5 to set up your credentials according to this [guide](http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/#setting-up-credentialsjson-file), or find and edit the `credentials.json` file manually.

Choose `6` \(exit\) if you would like to pause. Otherwise, continue.

## Running WizBot

If you aren't seeing the six options in terminal, run `cd ~ && wget -N https://github.com/Wizkiller96/WizBot-BashScript/raw/1.9/linuxAIO.sh && bash linuxAIO.sh`.

**The options:** `2. Run WizBot (Normally)`

If you shut down the bot with `.die`, it will stay offline until you manually run it again.

`3. Run WizBot with Auto Restart`

If you shut down the bot with `.die`, it will restart automatically. To stop the bot, stop the bot proccess \(close the terminal\)

Option 3 will show you some more options:

* `1. Run Auto Restart normally without Updating.`: Bot will restart on `die` command and will not be downloading the latest build available.
* `2. Run Auto Restart and update WizBot.` Bot will restart and download the latest build available everytime `die` command is used.

## Running with terminal closed

**Create a new Session:**

`tmux new -s wizbot`  
This will create a new session named `wizbot` _\(you can replace “wizbot” with anything you prefer, as long as you remember your session name\)_

Run the bot in this session. Detatch the session: `^b d`

**Attatching a detatched session** `tmux a -t wizbot`

**Remember** that while running with Auto Restart, closing the terminal won't stop the bot proccess. To stop the bot from terminal: `tmux kill-session -t wizbot`

## Updating WizBot

* Stop the bot, and make sure it is not running.
* Create a new tmux session if you are using tmux.
* `cd ~ && wget -N https://github.com/Wizkiller96/WizBot-BashScript/raw/1.9/linuxAIO.sh && bash linuxAIO.sh`
* Choose `1` to update the bot with latest build available.
* Choose `2` or `3` to run the bot again.

## Doing a clean reinstall

* Make a backup of your credentials \(`~/WizBot/src/WizBot/credentials.json`\)
* Make a backup of the database \(`~/WizBot/src/WizBot/bin/Release/netcoreapp2.0/data/WizBot.db`\)
* Delete the WizBot folder
* Reinstall, replace the files you backed up, and run.

## Help! My music isn't working!

Make sure you have the [Google API Key](http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/#setting-up-your-api-keys) in your `credentials.json` If music still isn't working, try reinstalling ffmpeg:

* `brew options ffmpeg`
* `brew install ffmpeg --with-x --with-y --with-z` etc.
* `brew update && brew upgrade` \(Update formulae and Homebrew itself && Install newer versions of outdated packages\)
* `brew prune` \(Remove dead symlinks from Homebrew’s prefix\)
* `brew doctor` \(Check your Homebrew installation for common issues\)
* Then try `brew install ffmpeg` again.

