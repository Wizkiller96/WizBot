## Setting up your API keys

This part is completely optional, **however it's necessary for music and a few other features to work properly**.

- **GoogleAPIKey**
    - Required for Youtube Song Search, Playlist queuing, and a few more things.
    - Follow these steps on how to setup Google API keys:
        - Go to [Google Console][Google Console] and log in.
        - Create a new project (name does not matter).
        - Once the project is created, go into `Library`
        - Under the `YouTube APIs` section, enable `YouTube Data API`
        - On the left tab, access `Credentials`,
            - Click `Create Credentials` button,
            - Click on `API Key`
            - A new window will appear with your `Google API key`  
              *NOTE: You don't really need to click on `RESTRICT KEY`, just click on `CLOSE` when you are done.*
            - Copy the key.
        - Open up `creds.yml` and look for `GoogleAPIKey`, paste your API key after the `:`.
        - It should look like this:
        ```yml
        "GoogleApiKey": "AIzaSyDSci1sdlWQOWNVj1vlXxxxxxbk0oWMEzM",
        ```
- **MashapeKey**
    - Required for Hearthstone cards.
    - Api key obtained on https://rapidapi.com (register -> go to MyApps -> Add New App -> Enter Name -> Application key)
    - Copy the key and paste it into `creds.yml`
- **OsuApiKey**
    - Required for Osu commands
    - You can get this key [here](https://osu.ppy.sh/p/api).
- **CleverbotApiKey**
    - Required if you want to use Cleverbot. It's currently a paid service.
    - You can get this key [here](http://www.cleverbot.com/api/).
- **PatreonAccessToken**
    - For Patreon creators only.
- **PatreonCampaignId**
    - For Patreon creators only. Id of your campaign.
- **TwitchClientId**
    - Mandatory for following twitch streams with `.twitch` (or `.stadd` with twitch link)
    - Go to [apps page](https://dev.twitch.tv/console/apps/create) on twitch and register your application.
    - You need 2FA enabled on twitch in order to create an application
    - You can set `http://localhost` as the OAuth Redirect URL (and press Add button)
    - Select `Chat Bot` from the Category dropdown
    - Once created, clicking on your application will show a new Client ID field
    - Copy it to your creds.yml as shown below
    - *(if you're adding it as the last key inside your creds.yml, remove the trailling comma from the example below)*
    ```yml
        TwitchClientId: "516tr61tr1qweqwe86trg3g",
    ```
- **LocationIqApiKey**
    - Optional. Used only for the `.time` command. https://locationiq.com api key (register and you will receive the token in the email). 
- **TimezoneDbApiKey**
    - Optional. Used only for the `.time` command. https://timezonedb.com api key (register and you will receive the token in the email **YOU HAVE TO ACTIVEATE IT AFTER YOU GET IT**).
- **CoinmarketcapApiKey**
    - Optional. Used only for the `.crypto` command. You can use crypto command without it, but you might get ratelimited from time to time, as all self-hosters share the default api key. https://pro.coinmarketcap.com/

##### Additional Settings

- **TotalShards**
    - Required if the bot will be connected to more than 2500 servers.
    - Most likely unnecessary to change until your bot is added to more than 2500 servers.
- **RedisOptions**
    - Required if the Redis instance is not on localhost or on non-default port.
    - You can find all available options [here](https://stackexchange.github.io/StackExchange.Redis/Configuration.html).
- **RestartCommand**
    - Required if you want to be able to use the `.restart` command
    - If you're using the CLI installer or Linux/OSX, it's easier and more reliable setup Nadeko with auto-restart and just use `.die`

For Windows (Updater), add this to your `creds.yml`

```yml
RestartCommand:
    Cmd: "NadekoBot.exe"
```

For Windows (Source), Linux or OSX, add this to your `creds.yml`

```yml
"RestartCommand": {
    "Cmd": "dotnet",
    "Args": "run -c Release"
},
```

---

#### End Result

**This is an example of how the `creds.yml` looks like with multiple owners, the restart command (optional) and all the API keys (also optional):**

```yml
{
  "Token": "MTc5MzcyXXX2MDI1ODY3MjY0.ChKs4g.I8J_R9XX0t-QY-0PzXXXiN0-7vo",
  "OwnerIds": [
        105635123466156544,
        145521851676884992,
        341420590009417729
  ],
  "GoogleApiKey": "AIzaSyDSci1sdlWQOWNVj1vlXxxxxxbk0oWMEzM",
  "MashapeKey": "4UrKpcWXc2mshS8RKi00000y8Kf5p1Q8kI6jsn32bmd8oVWiY7",
  "OsuApiKey": "4c8c8fdff8e1234581725db27fd140a7d93320d6",
  "CleverbotApiKey": "",
  "Db": null,
  "TotalShards": 1,
  "PatreonAccessToken": "",
  "PatreonCampaignId": "334038",
  "RestartCommand": {
    "Cmd": "NadekoBot.exe"
	},
  "ShardRunCommand": "",
  "ShardRunArguments": "",
  "ShardRunPort": null,
  "TwitchClientId": null,
  "RedisOptions": null
}
```

---

## Database

Nadeko saves all settings and data in the database file `NadekoBot.db`, located in:

- Windows (Updater): `system/data` (can be easily accessed through the `Data` button on the updater)
- Windows (Source), Linux and OSX: `NadekoBot/src/NadekoBot/bin/Release/netcoreapp2.1/data/NadekoBot.db`

In order to open it you will need [SQLite Browser](http://sqlitebrowser.org/).

*NOTE: You don't have to worry if you don't have the `NadekoBot.db` file, it gets automatically created once you successfully run the bot for the first time.*

**To make changes:**

- Shut your bot down.
- Copy the `NadekoBot.db` file to someplace safe. (Back up)
- Open it with SQLite Browser.
- Go to the **Browse Data** tab.
- Click on the **Table** drop-down list.
- Choose the table you want to edit.
- Click on the cell you want to edit.
- Edit it on the right-hand side.
- Click on **Apply**.
- Click on **Write Changes**.

![nadekodb](https://cdn.discordapp.com/attachments/251504306010849280/254067055240806400/nadekodb.gif)

---

## Sharding your bot

- **ShardRunCommand**
    - Command with which to run shards 1+
    - Required if you're sharding your bot on windows using .exe, or in a custom way.
    - This internally defaults to `dotnet`
    - For example, if you want to shard your NadekoBot which you installed using windows installer, you would want to set it to something like this: `C:\Program Files\NadekoBot\system\NadekoBot.exe`
- **ShardRunArguments**
    - Arguments to the shard run command
    - Required if you're sharding your bot on windows using .exe, or in a custom way.
    - This internally defaults to `run -c Release --no-build -- {0} {1} {2}` which will be enough to run linux and other 'from source' setups
    - {0} will be replaced by the `shard ID` of the shard being ran, {1} by the shard 0's process id, and {2} by the port shard communication is happening on
    - If shard0 (main window) is closed, all other shards will close too
    - For example, if you want to shard your NadekoBot which you installed using windows installer, you would want to set it to `{0} {1} {2}`
- **ShardRunPort**
    - Bot uses a random UDP port in [5000, 6000] range for communication between shards

[Google Console]: https://console.developers.google.com
[DiscordApp]: https://discordapp.com/developers/applications/me
[Invite Guide]: https://tukimoop.pw/s/guide.html
