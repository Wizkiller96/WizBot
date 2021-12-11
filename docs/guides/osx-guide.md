## MacOS From Source 

Open Terminal (if you don't know how to, click on the magnifying glass on the top right corner of your screen and type **Terminal** on the window that pops up) and navigate to the location where you want to install the bot (for example `cd ~`) 

##### Installing Homebrew, wget and dotnet

###### Homebrew/wget
*Skip this step if you already have homebrew installed*
- Copy and paste this command, then press Enter:  
    - `/usr/bin/ruby -e "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install)"`  
- Install wget  
   - `brew install wget`

###### Dotnet
- Download [.net5 SDK](https://dotnet.microsoft.com/download/dotnet/5.0)
- Open the `.pkg` file you've downloaded and install it.
- Run this command in Terminal. There might be output. If there is, disregard it. (copy-paste the entire block)
```bash
sudo mkdir /usr/local/bin

sudo mkdir /usr/local/lib
```
- Run this command in Terminal. There won't be any output. (copy-paste the entire block):
```bash
sudo ln -s /usr/local/share/dotnet/dotnet /usr/local/bin

sudo ln -s /usr/local/opt/openssl/lib/libcrypto.1.0.0.dylib /usr/local/lib/

sudo ln -s /usr/local/opt/openssl/lib/libssl.1.0.0.dylib /usr/local/lib/
```

##### Installation Instructions

1. Download and run the **new** installer script `cd ~ && wget -N https://github.com/Wizkiller96/wizbot-bash-installer/raw/master/linuxAIO.sh && bash linuxAIO.sh`
2. Install prerequisites (type `1` and press enter)
3. Download the bot (type `2` and press enter)
4. Exit the installer in order to set up your `creds.yml` 
5. Copy the creds.yml template 
    `cp wizbot/output/creds_example.yml wizbot/output/creds.yml` 
6. Open `wizbot/output/creds.yml` with your favorite text editor. We will use nano here
    - `nano wizbot/output/creds.yml`
7. [Enter your bot's token](#creds-guide)
    - After you're done, you can close nano (and save the file) by inputting, in order 
      - `CTRL`+`X`
      - `Y`
      - `Enter`
8. Run the bot (type `3` and press enter)

##### Update Instructions

1. ⚠ Stop the bot
2. Update and run the **new** installer script `cd ~ && wget -N https://github.com/Wizkiller96/wizbot-bash-installer/raw/master/linuxAIO.sh && bash linuxAIO.sh`
3. Update the bot (type `2` and press enter)
4. Run the bot (type `3` and press enter)
5. 🎉 

## MacOS Manual Release installation instructions

##### Installation Instructions

1. Download the latest release from <https://github.com/Wizkiller96/WizBot/releases>
   - Look for the file called "X.XX.X-linux-x64-build.tar" (where X.XX.X is a series of numbers) and download it
2. Untar it 
   ⚠ Make sure that you change X.XX.X to the same series of numbers as in step 1!
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
       - `CTRL`+`X`
       - `Y`
       - `Enter`
9. Run the bot
    - `./WizBot`

##### Update Instructions

1. Stop the bot
2. Download the latest release from <https://github.com/Wizkiller96/WizBot/releases>
    - Look for the file called "X.XX.X-linux-x64-build.tar" (where X.XX.X is a series of numbers) and download it
3. Untar it 
   ⚠ Make sure that you change X.XX.X to the same series of numbers as in step 2!
    - `tar xf 2.99.8-linux-x64-build.tar`
4. Rename the old wizbot directory to wizbot-old (remove your old backup first if you have one, or back it up under a different name)
    - `rm -rf wizbot-old 2>/dev/null`
    - `mv wizbot wizbot-old`
5. Rename the new wizbot directory to wizbot
    - `mv wizbot-linux-x64 wizbot`
6. Remove old strings and aliases to avoid overwriting the updated versions of those files  
   ⚠ If you've modified said files, back them up instead
    - `rm wizbot-old/data/aliases.yml`
    - `rm -r wizbot-old/data/strings`
7. Copy old data
    - `cp -RT wizbot-old/data/ wizbot/data/`
8. Copy creds.yml
    - `cp wizbot-old/creds.yml wizbot/`
9. Move into wizbot directory and make the wizBot executable
    - `cd wizbot && chmod +x wizBot`
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
cp -RT wizbot-old/data/ wizbot/data/ && \
cp wizbot-old/creds.yml wizbot/ && \
cd wizbot && chmod +x WizBot
```