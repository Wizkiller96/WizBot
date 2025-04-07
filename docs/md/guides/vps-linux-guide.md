## Setting up WizBot on a Linux VPS (Digital Ocean Droplet)

If you want WizBot to play music for you 24/7 without having to hosting it on your PC and want to keep it cheap, reliable and convenient as possible, you can try WizBot on Linux Digital Ocean Droplet using the link [DigitalOcean](https://m.do.co/c/7290047d0c84) (by using this link, you will get **$10 credit** and also support WizBot)

To set up the VPS, please select the options below
```
These are the min requirements you must follow:

OS: Any between Ubuntu, Fedora, and Debian

Droplet Type: SHARED CPU | Basic

CPU options: Regular | Disk type: SSD
6$/mo
1 GB / 1 CPU
25 GB SSD Disk
1000 GB transfer

Note: You can select the cheapest option with 512 MB / 1 CPU but this has been a hit or miss.

Datacenter region: Choose one depending on where you are located.

Authentication: Password or SSH
(Select SSH if you know what you are doing, otherwise choose password)
Click create droplet
```
**Setting up WizBot**
Assuming you have followed the link above to setup an account and a Droplet with a 64-bit operational system on Digital Ocean and got the `IP address and root password (in your e-mail)` to login, it's time to get started.

**This section is only relevant to those who want to host WizBot on DigitalOcean. Go through this whole section before setting the bot up.**

### Prerequisites

- Download [PuTTY](http://www.chiark.greenend.org.uk/~sgtatham/putty/download.html)
- Download [WinSCP](https://winscp.net/eng/download.php) *(optional)*
- [Create and invite the bot](../creds-guide.md).

### Starting up

- **Open PuTTY** and paste or enter your `IP address` and then click **Open**.
  If you entered your Droplets IP address correctly, it should show **login as:** in a newly opened window.
- Now for **login as:**, type `root` and press enter.
- It should then ask for a password. Type the `root password` you have received in your e-mail address, then press Enter.

If you are running your droplet for the first time, it will most likely ask you to change your root password. To do that, copy the **password you've received by e-mail** and paste it on PuTTY.

- To paste, just right-click the window (it won't show any changes on the screen), then press Enter.
- Type a **new password** somewhere, copy and paste it on PuTTY. Press Enter then paste it again.

**Save the new password somewhere safe.**

After that, your droplet should be ready for use.

[Setting up WizBot on a VPS (Digital Ocean)]: #setting-up-wizbot-on-a-linux-vps-digital-ocean-droplet
