using Discord.Commands;
using WizBot.Modules;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WizBot.Classes.Conversations.Commands
{
    internal class CopyCommand : DiscordCommand
    {
        private readonly HashSet<ulong> CopiedUsers = new HashSet<ulong>();

        public CopyCommand(DiscordModule module) : base(module)
        {
            WizBot.Client.MessageReceived += Client_MessageReceived;
        }

        private async void Client_MessageReceived(object sender, Discord.MessageEventArgs e)
        {
            if (e.User.Id == WizBot.Client.CurrentUser.Id) return;
            try
            {
                if (string.IsNullOrWhiteSpace(e.Message.Text))
                    return;
                if (CopiedUsers.Contains(e.User.Id))
                {
                    await e.Channel.SendMessage(e.Message.Text).ConfigureAwait(false);
                }
            }
            catch { }
        }

        public Func<CommandEventArgs, Task> DoFunc() => async e =>
        {
            if (CopiedUsers.Contains(e.User.Id)) return;

            CopiedUsers.Add(e.User.Id);
            await e.Channel.SendMessage(" I'll start copying you now.").ConfigureAwait(false);
        };

        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand("copyme")
                .Alias("cm")
                .Description("Wiz Bot starts copying everything you say. Disable with cs")
                .Do(DoFunc());

            cgb.CreateCommand("cs")
                .Alias("copystop")
                .Description("Wiz Bot stops copying you")
                .Do(StopCopy());
        }

        private Func<CommandEventArgs, Task> StopCopy() => async e =>
        {
            if (!CopiedUsers.Contains(e.User.Id)) return;

            CopiedUsers.Remove(e.User.Id);
            await e.Channel.SendMessage(" I wont copy anymore.").ConfigureAwait(false);
        };
    }
}
