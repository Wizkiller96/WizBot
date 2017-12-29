#Frequently Asked Questions


###Question 1: How do I get WizBot to join my server?
----
**Answer:** Simply send WizBot a Direct Message with `.h` and follow the link. **Only People with the Manage Server permission can add the bot to the server**

###Question 2: I want to change permissions, but it isn't working!
----
**Answer:** You must have the `.permrole` (by default this is the `WizBot` role, for more details on permissions check [here](http://wizbot.readthedocs.io/en/latest/Permissions%20System/ "Permissions"). If you have a role called `WizBot` but can't assign it it's probably the Bot Role so, just create a **New Role** called `WizBot` and assign that to yourself instead.)

###Question 3: I want to enable NSFW on a channel.
----
**Answer:** To enable the NSFW Module on one channel, type `.cm NSFW enable #channel-name`. If this does not work refer to Question 2. To enable NSFW for your **entire server**, type `.sm NSFW enable`.

###Question 4: How do I get Cherry Blossom/Currency?
----
**Answer:** You can get Cherry Blossom by picking them up after they have been generated with `.gc`, which you can then either plant (give away to a channel so that someone can pick it), or gamble with for potentinal profit with `.betflip`, `.betroll` and `.jr`. You can get flowers on the public bot by reacting on reaction events or by donating on [Patreon](https://patreon.com/nadekobot) or [PayPal](https://paypal.me/Kwoth). 

###Question 5: I have an issue/bug/suggestion, where do I put it so it gets noticed?
-----------
**Answer:** First, check [issues](https://github.com/Wizkiller96/WizBot/issues "GitHub WizBot Issues").

If your problem or suggestion is not there, feel free to request/notify us about it in the Issues section of GitHub for issues.

###Question 6: How do I use this command?
--------
**Answer:** You can see the description and usage of certain commands by using `.h command` **i.e** `.h .sm`. 

The whole list of commands can be found [here](http://wizbot.readthedocs.io/en/latest/Commands%20List/ "Command List")

###Question 7: Music isn't working?
----
**Answer:** Music is disabled on public WizBot due to large hosting costs, it will be re-enabled later in the future for donators. 

**If you would like music in the meantime, you must host WizBot yourself**. Be sure you have FFMPEG installed correctly, and have followed the [guide](http://wizbot.readthedocs.io/en/latest/guides/Windows%20Guide/#setting-up-wizbot-for-music) carefully.

###Question 8: My music is still not working/very laggy?
----
**Answer:** Try changing your discord [location][1], if this doesn't work be sure you have enabled the correct permissions for WizBot and rebooted since installing FFMPEG.
[1]: https://support.discordapp.com/hc/en-us/articles/216661717.how-do-I-change-my-Voice-Server-Region-

###Question 9: I want to change data in the database like Cherry Blossom or something else but how?
----
**Answer:** Follow the [DB Guide](http://wizbot.readthedocs.io/en/latest/JSON%20Explanations/#db-files), Open `/data/WizBot.db` using **sqlitebrowser** (or some alternative), Browse Data, select relevant table, change data, Write changes and done.

###Question 10: The .greet and .bye commands doesn't work, but everything else is!
-----
**Answer:** Set a greeting message by using `.greetmsg YourMessageHere` and a bye-message by using `.byemsg YourMessageHere`. Don't forget that `.greet` and `.bye` only apply to users joining a server, not coming online/offline.

###Question 11:  I made an application, but I can't add that new bot to my server, how do I invite it to my server?
----
**Answer:** You need to use oauth link to add it to you server, just copy your **CLIENT ID** (that's in the same [Developer page](https://discordapp.com/developers/applications/me) where you brought your token) and replace `12345678` in the link below: **https://discordapp.com/oauth2/authorize?client_id=`12345678`&scope=bot&permissions=66186303**

Follow this Detailed [Guide](http://wizbot.cf/invite-guide.html).

###Question 12:  I'm building WizBot from source, but I get hundreds of (namespace) errors without changing anything!?
-----
**Answer:** Using Visual Studio, you can solve these errors by going to `Tools` -> `NuGet Package Manager` -> `Manage NuGet Packages for Solution`. Go to the Installed tab, select the Packages that were missing (usually `Newtonsoft.json` and `RestSharp`) and install them for all projects.

###Question 13:  My bot has all permissions but it's still saying, "Failed to add roles. Bot has insufficient permissions". How do I fix this?
----------
**Answer:** Discord has added a few new features and the roles now follow the role hierarchy, which means you need to place your bot's role above every other role on your server to fix the issue. [Here's](https://support.discordapp.com/hc/en-us/articles/214836687-Role-Management-101) a link to Discord's role management 101.

**Please Note:** *The bot can only set/add all roles below its own highest role. It cannot assign its "highest role" to anyone else.*

###Question 14: I've broken permissions and am stuck, can I reset permissions?
----------
**Answer:** Yes, there is a way, in one easy command! Just run `.resetperms` and all the permissions you've set through **Permissions Module** will reset.