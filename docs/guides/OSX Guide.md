## Setting Up WizBot on OSX

#### Prerequisites 
- [Homebrew][Homebrew]
- Google Account
- Soundcloud Account (if you want soundcloud support)
- Text Editor (TextWrangler, or equivalent) or outside editor such as [Atom][Atom]

####Installing Homebrew

```/usr/bin/ruby -e "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install)"```

Run `brew update` to fetch the latest package data.  

####Installing dependencies
```
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
```

####Installing .NET Core SDK

- `ln -s /usr/local/opt/openssl/lib/libcrypto.1.0.0.dylib /usr/local/lib/`
- `ln -s /usr/local/opt/openssl/lib/libssl.1.0.0.dylib /usr/local/lib/`
- Download the [.NET Core SDK](https://www.microsoft.com/net/core#macos), found [here.](https://go.microsoft.com/fwlink/?LinkID=835011)
- Open the `.pkg` file you downloaded and install it.
- `ln -s /usr/local/share/dotnet/dotnet /usr/local/bin`

####Check your `FFMPEG`

**In case your `FFMPEG` wasnt installed properly (Optional)**

- `brew options ffmpeg`
- `brew install ffmpeg --with-x --with-y --with-z` etc.
- `brew update && brew upgrade` (Update formulae and Homebrew itself && Install newer versions of outdated packages)
- `brew prune` (Remove dead symlinks from Homebrew’s prefix)
- `brew doctor` (Check your Homebrew installation for common issues)
- Then try `brew install ffmpeg` again.

####Installing xcode-select

Xcode command line tools. You will do this in Terminal.app by running the following command line:

`xcode-select --install`

A dialog box will open asking if you want to install `xcode-select`. Select install and finish the installation.

####Downloading and building Nadeko

Use the following command to get and run `linuxAIO.sh`:		
(Remember **DO NOT** rename the file `linuxAIO.sh`)

`cd ~ && wget https://github.com/Wizkiller96/WizBot-BashScript/raw/master/linuxAIO.sh && bash linuxAIO.sh`

Follow the on screen instructions:

1. To Get the latest build. (most recent updates)
2. To Get the stable build.

Choose either `1` or `2` then press `enter` key.	
Once Installation is completed you should see the options again.	
Next, choose `5` to exit. 

####Creating and Inviting bot

- Read here how to [create a DiscordBot application](N/A)
- [Visual Invite Guide](http://discord.kongslien.net/guide.html) *NOTE: Client ID is your Bot ID*
- Copy your `Client ID` from your [applications page](https://discordapp.com/developers/applications/me).
- Replace the `12345678` in this link `https://discordapp.com/oauth2/authorize?client_id=12345678&scope=bot&permissions=66186303` with your `Client ID`.
- The link should now look like this: `https://discordapp.com/oauth2/authorize?client_id=**YOUR_CLENT_ID_HERE**&scope=bot&permissions=66186303`.
- Go to the newly created link and pick the server we created, and click `Authorize`.
- The bot should have been added to your server.
 
####Setting up Credentials.json file
- Open up the `WizBot` folder, which should be in your home directory, then `WizBot` folder then `src` folder and then the additonal `WizBot` folder.
- EDIT it as it is guided here: [Setting up credentials.json](N/A)
- **If** you already have WizBot 1.0 setup and have `credentials.json` and `WizBot.db`, you can just copy and paste the `credentials.json` to `WizBot/src/WizBot` and `WizBot.db` to `WizBot/src/WizBot/bin/Release/netcoreapp1.0/data`.
- **If** you have WizBot 0.9x follow the [Upgrading Guide](N/A)

####Setting WizBot Music

For Music Setup and API keys check [Setting up WizBot for Music](N/A) and [JSON Explanations](N/A).

####Running WizBot

- Using tmux

`tmux new -s wizbot`

^this will create a new session named “wizbot”  
`(you can replace “wizbot” with anything you prefer and remember its your session name)`.

- Using Screen

`screen -S wizbot`

^this will create a new screen named “wizbot”  
`(you can replace “wizbot” with anything you prefer and remember its your screen name)`.

- Start WizBot using .NET Core:

`cd ~ && bash linuxAIO.sh`

From the options,

Choose `3` To Run the bot normally.		
**NOTE:** With option `3` (Running Normally), if you use `.die` [command](N/A) in discord. The bot will shut down and will stay offline untill you manually run it again. (best if you want to check the bot.)

Choose `4` To Run the bot with Auto Restart.	
**NOTE:** With option `4` (Running with Auto Restart), bot will auto run if you use `.die` [command](N/A) making the command `.die` to be used as restart.	
**NOTE:** [To stop the bot you will have to kill the session.](N/A)

**Now check your Discord, the bot should be online**

Now time to move bot to background and to do that, press CTRL+B+D (this will detach the wizbot session using TMUX)	
If you used Screen press CTRL+A+D (this will detach the wizbot screen) 

####Updating WizBot

- Connect to the terminal.
- `tmux kill-session -t wizbot` [(don't forget to replace **wizbot** in the command to what ever you named your bot's session)](N/A)
- Make sure the bot is **not** running.
- `tmux new -s wizbot` (**wizbot** is the name of the session)
- `cd ~ && bash linuxAIO.sh`
- Choose either `1` or `2` to update the bot with **latest build** or **stable build** respectively.
- Choose either `3` or `4` to run the bot again with **normally** or **auto restart** respectively.
- Done. You can close terminal now.

####Some more Info

**TMUX**

- If you want to see the sessions after logging back again, type `tmux ls`, and that will give you the list of sessions running. 
- If you want to switch to/ see that session, type `tmux a -t wizbot` (wizbot is the name of the session we created before so, replace `wizbot` with the session name you created.)
- If you want to kill WizBot session, type `tmux kill-session -t wizbot`

**Screen**

- If you want to see the sessions after logging back again, type `screen -ls`, and that will give you the list of screens. 
- If you want to switch to/ see that screen, type `screen -r wizbot` (wizbot is the name of the screen we created before so, replace `wizbot` with the screen name you created.)
- If you want to kill the WizBot screen, type `screen -X -S wizbot quit`

####Alternative Method to Install WizBot

**METHOD I**

- `cd ~ && curl -L https://github.com/Wizkiller96/WizBot-BashScript/raw/master/wizbot_installer.sh | sh`

**METHOD II**

- `cd ~`
- `git clone -b 1.0 --recursive https://github.com/Wizkiller96/WizBot.git`
- `cd ~/WizBot/discord.net`
- `dotnet restore -s https://dotnet.myget.org/F/dotnet-core/api/v3/index.json`
- `dotnet restore`
- `cd ~/WizBot/src/WizBot/`
- `dotnet restore` 
- `dotnet build --configuration Release`

[Homebrew]: http://brew.sh/
[DiscordApp]: https://discordapp.com/developers/applications/me
[Atom]: https://atom.io/
[Invite Guide]: http://discord.kongslien.net/guide.html
[Google Console]: https://console.developers.google.com
[Soundcloud]: https://soundcloud.com/you/apps/new
