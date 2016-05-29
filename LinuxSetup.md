#SETTING UP WIZBOT ON LINUX UBUNTU 14+

######If you want WizBot to play music for you 24/7 without having to hosting it on your PC and want to keep it cheap, reliable and convenient as possible, you can try WizBot on Wiz VPS KVM server using the link http://wiz-vps.com/vps-hosting-plans/kvm/


Assuming you have followed the link above to created an account in Wiz VPS and until you get the `IP address and root password (in email)` to login, its time to begin:

**DOWNLOAD PuTTY**

http://www.chiark.greenend.org.uk/~sgtatham/putty/download.html

**DOWNLOAD and INSTALL CyberDuck** `(for accessing filesystem using SFTP)`

https://cyberduck.io



**Follow the steps below:**

**Open PuTTY.exe** that you downloaded before, and paste or enter your `IP address` and then click **Open**

If you entered your VPS IP address correctly, it should show **login as:** in a newly opened window.

Now for **login as:**, type `root` and hit enter.

It should then, ask for password, type the `root password` you have received in your **email address registered with Wiz VPS**, then hit Enter

*(as you are running it for the first time, it will most likely to ask you to change your root password, for that, type the "password you received through email", hit Enter, enter a "new password", hit Enter and confirm that "new password" again.*
**SAVE that new password somewhere safe not just in mind**

After you done that, you are ready to write commands.

**Copy and just paste** using **mouse right-click** (it should paste automatically)

######MONO (Source: http://www.mono-project.com/docs/getting-started/install/linux/)

**1)**

<pre><code class="language-bash">sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
echo "deb http://download.mono-project.com/repo/debian wheezy main" | sudo tee /etc/apt/sources.list.d/mono-xamarin.list
sudo apt-get update
</code></pre>
Note if the command is not be initiated, hit **Enter**

**2)**
<pre><code class="language-bash">echo "deb http://download.mono-project.com/repo/debian wheezy-apache24-compat main" | sudo tee -a /etc/apt/sources.list.d/mono-xamarin.list
</code></pre>

**2.5)**
*ONLY DEBIAN 8 and later*
<pre><code class="language-bash">echo "deb http://download.mono-project.com/repo/debian wheezy-libjpeg62-compat main" | sudo tee -a /etc/apt/sources.list.d/mono-xamarin.list
</code></pre>

**3)**
<pre><code class="language-bash">apt-get install mono-devel
</code></pre>
**Type** `y` **hit Enter**
######Opus Voice Codec

**4)**
<pre><code class="language-bash">sudo apt-get install libopus0 opus-tools
</code></pre>
**Type** `y` **hit Enter**

**5)**
<pre><code class="language-bash">sudo apt-get install libopus-dev
</code></pre>

######FFMPEG

**6)**
<pre><code class="language-bash">apt-get install ffmpeg
</code></pre>
**Type** `y` **hit Enter**

`NOTE: if its "not installing" then, follow the guide here:` http://www.faqforge.com/linux/how-to-install-ffmpeg-on-ubuntu-14-04/

*All you need to do, if you are running UBUNTU 14.04 is initiate these:*

`sudo add-apt-repository ppa:mc3man/trusty-media`

`sudo apt-get update`

`sudo apt-get dist-upgrade`

*Before executing* `sudo apt-get install ffmpeg`

######Uncomplicated Firewall UFW

**7)**
<pre><code class="language-bash">apt-get install ufw
</code></pre>
**it is most likely to have it already installed so if you see it is already installed, check with following command, and/or enable it**

**8)**
<pre><code class="language-bash">ufw status
</code></pre>

**9)**
<pre><code class="language-bash">ufw enable
</code></pre>
**Type** `y` **hit Enter**

**10)**
<pre><code class="language-bash">sudo ufw allow ssh
</code></pre>

######Unzip

**11)**
<pre><code class="language-bash">apt-get install unzip
</code></pre>

######TMUX
**12)**
<pre><code class="language-bash">apt-get install tmux
</code></pre>
**Type** `y` **hit Enter**

######NOW WE NEED TO IMPORT SOME DISCORD CERTS
**13)**
<pre><code class="language-bash">mozroots --import --ask-remove --machine
</code></pre>

**14)**
<pre><code class="language-bash">certmgr --ssl https://gateway.discord.gg
</code></pre>

Type `yes` and hit Enter **(three times - as it will ask for three times)**


**15)**

Create a new folder “wizbot” or anything you prefer
<pre><code class="language-bash">mkdir wizbot
</code></pre>

**16)**

Move to “wizbot” folder (note `cd --` to go back the directory)
<pre><code class="language-bash">cd wizbot
</code></pre>

**NOW WE NEED TO GET WIZBOT FROM RELEASES**


Go to this link: https://github.com/Wizkiller96/WizBot-Updated/releases and **copy the zip file address** of the lalest version available,

it should look like `https://github.com/Wizkiller96/WizBot-Updated/releases/download/vx.xx/WizBot.vx.x.zip`

**17)**

Get the correct link, type `wget`, then *paste the link*, then hit **Enter**.
<pre><code class="language-bash">wget https://github.com/Wizkiller96/WizBot-Updated/releases/download/vx.xx/WizBot.vx.x.zip
</code></pre>
**^Do not copy-paste it**

**18)**

Now we need to `unzip` the downloaded zip file and to do that, type the file name as it showed in your screen or just copy from the screen, should be like ` NadekoBot.vx.x.zip`
<pre><code class="language-bash">unzip WizBot.vx.x.zip
</code></pre>
**^Do not copy-paste it**

######NOW TO SETUP WIZBOT

Open **CyberDuck**

Click on **Open Connection** (top-left corner), a new window should appear.

You should see **FTP (File Transfer Protocol)** in drop-down.

Change it to **SFTP (SSH File Transfer Protocol)**

Now, in **Server:** paste or type in your `Digital Ocean Droplets IP address`, leave `Port: 22` (no need to change it)

In **Username:** type `root`

In **Password:** type `the new root password (you changed at the start)`

Click on **Connect**

It should show you the new folder you created.

Open it.

######MAKE SURE YOU READ THE README BEFORE PROCEEDING

Copy the `credentials_example.json` to desktop

EDIT it as it is guided here: https://github.com/Wizkiller96/WizBot-Updated/blob/master/README.md

Rename it to `credentials.json` and paste/put it back in the folder. `(Yes, using CyberDuck)`

You should see two files `credentials_example.json` and `credentials.json`

Also if you already have wizbot setup and have `credentials.json`, `config.json`, `WizBot.sqlite`, and `"permissions" folder`, you can just copy and paste it to the Droplets folder using CyberDuck.

######TIME TO RUN

Go back to **PuTTY**, `(hope its still running xD)`

**19)**

Type/ Copy and hit **Enter**.
<pre><code class="language-bash">tmux new -s wizbot
</code></pre>
**^this will create a new session named “wizbot”** `(you can replace “wizbot” with anything you prefer and remember its your session name) so you can run the bot in background without having to keep running PuTTY in the background.`


<pre><code class="language-bash">cd wizbot
</code></pre>

**20)**

<pre><code class="language-bash">mono WizBot.exe
</code></pre>

**CHECK THE BOT IN DISCORD, IF EVERYTHING IS WORKING**

Now time to **move bot to background** and to do that, press **CTRL+B+D** (this will ditach the nadeko session using TMUX), and you can finally close PuTTY now.

**NOW YOU HAVE YOUR OWN WIZBOT**

######SOME MORE INFO (JUST TO KNOW):

-If you want to **see the sessions** after logging back again, type `tmux ls`, and that will give you the list of sessions running.

-If you want to **switch to/ see that session**, type `tmux a -t wizbot` (**wizbot** is the name of the session we created before so, replace **“wizbot”** with the session name you created.)

**21)**

-If you want to **kill** WizBot **session**, type `tmux kill-session -t wizbot`

######TO RESTART YOUR BOT ALONG WITH THE WHOLE SERVER (for science):
**22)**

Open **PuTTY** and login as you have before, type `reboot` and hit Enter.

######IF YOU WANT TO UPDATE YOUR BOT

**FOLLOW THESE STEPS SERIALLY**

**-21 OR 22**

**-19**

**-16**

**-17**

**-18**

**-20**

HIT **CTRL+B+D** and close **PuTTY**
