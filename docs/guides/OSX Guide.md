## Setting Up WizBot on OSX (macOS)

#### Prerequisites 
- [Homebrew][Homebrew]
- [Google Account](http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/#setting-up-your-api-keys)
- Text Editor (TextWrangler, or equivalent) or outside editor such as [Atom][Atom]

#### Installing Homebrew

```/usr/bin/ruby -e "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install)"```

Run `brew update` to fetch the latest package data.  

#### Installing dependencies
```
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

#### Installing .NET Core SDK

- `ln -s /usr/local/opt/openssl/lib/libcrypto.1.0.0.dylib /usr/local/lib/`
- `ln -s /usr/local/opt/openssl/lib/libssl.1.0.0.dylib /usr/local/lib/`
- Download the [.NET Core SDK][.NET Core SDK]
- Open the `.pkg` file you downloaded and install it.
- `ln -s /usr/local/share/dotnet/dotnet /usr/local/bin`

#### Check your `FFMPEG`

**In case your `FFMPEG` wasnt installed properly (Optional)**

- `brew options ffmpeg`
- `brew install ffmpeg --with-x --with-y --with-z` etc.
- `brew update && brew upgrade` (Update formulae and Homebrew itself && Install newer versions of outdated packages)
- `brew prune` (Remove dead symlinks from Homebrew’s prefix)
- `brew doctor` (Check your Homebrew installation for common issues)
- Then try `brew install ffmpeg` again.

#### Installing xcode-select

Xcode command line tools. You will do this in Terminal.app by running the following command line:

`xcode-select --install`

A dialog box will open asking if you want to install `xcode-select`. Select install and finish the installation.

#### Downloading and building WizBot

Use the following command to get and run `linuxAIO.sh`:		
(Remember **DO NOT** rename the file `linuxAIO.sh`)

`cd ~ && wget -N https://github.com/Wizkiller96/WizBot-BashScript/raw/1.9/linuxAIO.sh && bash linuxAIO.sh`

Follow the on screen instructions:

Choose `1. Download WizBot` To Get the latest build. (most recent updates)

Once Installation is completed you should see the options again.	
Next, choose `6` to exit. 

#### Creating and Inviting bot

- Read here [how to create a Discord Bot application and invite it.](http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/#creating-discord-bot-application)
 
#### Setting up Credentials.json file
- Open up the `WizBot` folder, which should be in your home directory, then `WizBot` folder then `src` folder and then the additonal `WizBot` folder.
- Edit the way its guided here: [Setting up credentials.json](http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/#setting-up-credentialsjson-file)
- **If** you already have WizBot 1.x setup and have `credentials.json` and `WizBot.db`, you can just copy and paste the `credentials.json` to `WizBot/src/WizBot` and `WizBot.db` to `WizBot/src/WizBot/bin/Release/netcoreapp2.0/data`.			
**Or** follow the [Upgrading Guide.](http://wizbot.readthedocs.io/en/latest/guides/Upgrading%20Guide/)

#### Setting WizBot Music

For Music Setup and API keys check [Setting up WizBot for Music](http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/#setting-up-your-api-keys) and [JSON Explanations](http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/).

#### Running WizBot

**Create a new Session:**

- Using Screen			

`screen -S wizbot`
 
- Using tmux			

`tmux new -s wizbot`  
  
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


Now time to move bot to background and to do that, press CTRL+B,D (this will detach the wizbot session using TMUX)	
If you used Screen press CTRL+A+D (this will detach the wizbot screen) 

#### Updating WizBot

- Connect to the terminal.
- `tmux kill-session -t wizbot` [(don't forget to replace **wizbot** in the command to what ever you named your bot's session)](http://wizbot.readthedocs.io/en/latest/guides/OSX%20Guide/#some-more-info)
- Make sure the bot is **not** running.
- `tmux new -s wizbot` (**wizbot** is the name of the session)
- `cd ~ && wget -N https://github.com/Wizkiller96/WizBot-BashScript/raw/1.9/linuxAIO.sh && bash linuxAIO.sh`
- Choose `1` to update the bot with **latest build** available.
- Next, choose either `2` or `3` to run the bot again with **normally** or **auto restart** respectively.
- Done.

#### Some more Info

**TMUX**

- If you want to see the sessions after logging back again, type `tmux ls`, and that will give you the list of sessions running. 
- If you want to switch to/ see that session, type `tmux a -t wizbot` (wizbot is the name of the session we created before so, replace `wizbot` with the session name you created.)
- If you want to kill WizBot session, type `tmux kill-session -t wizbot`

**Screen**

- If you want to see the sessions after logging back again, type `screen -ls`, and that will give you the list of screens. 
- If you want to switch to/ see that screen, type `screen -r wizbot` (wizbot is the name of the screen we created before so, replace `wizbot` with the screen name you created.)
- If you want to kill the WizBot screen, type `screen -X -S wizbot quit`

[Homebrew]: http://brew.sh/
[.NET Core SDK]: https://www.microsoft.com/net/core#macos
[DiscordApp]: https://discordapp.com/developers/applications/me
[Atom]: https://atom.io/
[Invite Guide]: http://discord.kongslien.net/guide.html
[Google Console]: https://console.developers.google.com
[Soundcloud]: https://soundcloud.com/you/apps/new