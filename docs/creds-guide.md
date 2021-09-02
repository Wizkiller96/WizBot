## Creds Guide

This document aims to guide you through the process of creating a Discord account for your bot 
(the Discord Bot application), and inviting that account into your Discord server.

![Create a bot application and copy token to creds.yml file](https://cdn.nadeko.bot/tutorial/bot-creds-guide.gif)

- Go to [the Discord developer application page][DiscordApp].
- Log in with your Discord account.
- Click **New Application**.
- Fill out the `Name` field however you like.
- Go to the **Bot** tab on the left sidebar.
- Click on the `Add a Bot` button and confirm that you do want to add a bot to this app.
- **Optional:** Add bot's avatar and description.
- Copy your Token to `creds.yml` as shown above.
- Scroll down to the `Privileged Gateway Intents` section and enable both intents.
  These are required for a number of features to function properly, and should both be on.

#### Inviting your bot to your server    

![Invite the bot to your server](https://cdn.nadeko.bot/tutorial/bot-invite-guide.gif)

- On the **General Information** tab, copy your `Application ID` from your [applications page][DiscordApp].
- Replace the `YOUR_CLIENT_ID_HERE` in this link:
  `https://discordapp.com/oauth2/authorize?client_id=YOUR_CLIENT_ID_HERE&scope=bot&permissions=66186303` with your `Client ID`
- The link should now look something like this:
  `https://discordapp.com/oauth2/authorize?client_id=123123123123&scope=bot&permissions=66186303`
- Access that newly created link, pick your Discord server, click `Authorize` and confirm with the captcha at the end
- The bot should now be in your server

That's it! You may now go back to the installation guide you were following before 🎉

[DiscordApp]: https://discordapp.com/developers/applications/me