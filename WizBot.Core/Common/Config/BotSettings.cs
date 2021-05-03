using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace WizBot.Core.Common.Configs
{
    public class BotSettings
    {
        [YamlMember(Description = @"DO NOT CHANGE")]
        public int Version { get; set; }

        [YamlMember(Description = @"Most commands, when executed, have a small colored line
next to the response. The color depends whether the command
is completed, errored or in progress (pending)
Color settings below are for the color of those lines.
To get color's hex, you can go here https://htmlcolorcodes.com/
and copy the hex code fo your selected color (marked as #)")]
        public ColorConfig Color { get; set; }

        [YamlMember(Description = "Default bot language. It has to be in the list of supported languages (.langli)")]
        public string DefaultLocale { get; set; }

        [YamlMember(Description = @"Style in which executed commands will show up in the console.
Allowed values: Simple, Normal, None")]
        public ConsoleOutputType ConsoleOutputType { get; set; }

        //         [YamlMember(Description = @"For what kind of updates will the bot check.
        // Allowed values: Release, Commit, None")]
        //         public UpdateCheckType CheckForUpdates { get; set; }

        [YamlMember(Description = @"How often will the bot check for updates, in hours")]
        public int CheckUpdateInterval { get; set; }

        [YamlMember(Description = @"Do you want any messages sent by users in Bot's DM to be forwarded to the owner(s)?")]
        public bool ForwardMessages { get; set; }

        [YamlMember(Description = @"Do you want the message to be forwarded only to the first owner specified in the list of owners (in creds.yml),
or all owners? (this might cause the bot to lag if there's a lot of owners specified)")]
        public bool ForwardToAllOwners { get; set; }

        [YamlMember(Description = @"When a user DMs the bot with a message which is not a command
they will receive this message. Leave empty for no response. The string which will be sent whenever someone DMs the bot.
Supports embeds. How it looks: https://puu.sh/B0BLV.png")]
        public string DmHelpText { get; set; }

        [YamlMember(Description = @"This is the response for the .h command")]
        public string HelpText { get; set; }
        [YamlMember(Description = @"List of modules and commands completely blocked on the bot")]
        public BlockedConfig Blocked { get; set; }

        [YamlMember(Description = @"Which string will be used to recognize the commands")]
        public string Prefix { get; set; }
        [YamlMember(Description = @"Whether the bot will rotate through all specified statuses.
This setting can be changed via .rots command.
See RotatingStatuses submodule in Administration.")]
        public bool RotateStatuses { get; set; }

        [YamlMember(Description = @"Amount of currency user will receive for every CENT pledged on patreon.")]
        public float PatreonCurrencyPerCent { get; set; }

        [YamlMember(Description = @"Toggles whether your bot will group greet/bye messages into a single message every 5 seconds.
1st user who joins will get greeted immediately
If more users join within the next 5 seconds, they will be greeted in groups of 5.
This will cause %user.mention% and other placeholders to be replaced with multiple users. 
Keep in mind this might break some of your embeds - for example if you have %user.avatar% in the thumbnail,
it will become invalid, as it will resolve to a list of avatars of grouped users.")]
        public bool GroupGreets { get; set; }

        //         [YamlMember(Description = @"Whether the prefix will be a suffix, or prefix.
        // For example, if your prefix is ! you will run a command called 'cash' by typing either
        // '!cash @Someone' if your prefixIsSuffix: false or
        // 'cash @Someone!' if your prefixIsSuffix: true")]
        //         public bool PrefixIsSuffix { get; set; }

        // public string Prefixed(string text) => PrefixIsSuffix
        //     ? text + Prefix
        //     : Prefix + text;

        public string Prefixed(string text)
            => Prefix + text;

        public BotSettings()
        {
            Version = 1;
            var color = new ColorConfig();
            Color = color;
            DefaultLocale = "en-US";
            ConsoleOutputType = ConsoleOutputType.Simple;
            CheckUpdateInterval = 3;
            ForwardMessages = true;
            ForwardToAllOwners = true;
            DmHelpText = @"{""description"": ""Type `%prefix%h` for help.""}";
            HelpText = @"{
  ""title"": ""To invite me to your server, use this link"",
  ""description"": ""https://discordapp.com/oauth2/authorize?client_id={0}&scope=bot&permissions=66186303"",
  ""color"": 53380,
  ""thumbnail"": ""https://i.imgur.com/nKYyqMK.png"",
  ""fields"": [
    {
      ""name"": ""Useful help commands"",
      ""value"": ""`%bot.prefix%modules` Lists all bot modules.
`%prefix%h CommandName` Shows some help about a specific command.
`%prefix%commands ModuleName` Lists all commands in a module."",
      ""inline"": false
    },
    {
      ""name"": ""List of all Commands"",
      ""value"": ""https://commands.wizbot.cc/"",
      ""inline"": false
    },
    {
      ""name"": ""WizBot Support Server"",
      ""value"": ""https://wizbot.cc/discord "",
      ""inline"": true
    }
  ]
}";
            var blocked = new BlockedConfig();
            Blocked = blocked;
            Prefix = ".";
            RotateStatuses = false;
            PatreonCurrencyPerCent = 100;
            GroupGreets = false;
        }
    }

    public class BlockedConfig
    {
        [YamlMember(Description = @"")]
        public List<string> Commands { get; set; }
        [YamlMember(Description = @"")]
        public List<string> Modules { get; set; }

        public BlockedConfig()
        {
            Modules = new List<string>()
            {
                "nsfw"
            };
            Commands = new List<string>();
        }
    }

    public class ColorConfig
    {
        [YamlMember(Description = @"")]
        public string Ok { get; set; }
        [YamlMember(Description = @"")]
        public string Error { get; set; }
        [YamlMember(Description = @"")]
        public string Pending { get; set; }

        public ColorConfig()
        {
            Ok = "AB40CD";
            Error = "F04747";
            Pending = "FAA61A";
        }
    }
    public enum ConsoleOutputType
    {
        Normal = 0,
        Simple = 1,
        None = 2,
    }
}