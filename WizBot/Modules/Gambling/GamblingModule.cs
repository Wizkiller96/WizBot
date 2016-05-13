using Discord;
using Discord.Commands;
using Discord.Modules;
using WizBot.Classes;
using WizBot.DataModels;
using WizBot.Extensions;
using WizBot.Modules.Permissions.Classes;
using System;
using System.Linq;
using System.Text;

namespace WizBot.Modules.Gambling
{
    internal class GamblingModule : DiscordModule
    {

        public GamblingModule()
        {
            commands.Add(new DrawCommand(this));
            commands.Add(new FlipCoinCommand(this));
            commands.Add(new DiceRollCommand(this));
        }

        public override string Prefix { get; } = WizBot.Config.CommandPrefixes.Gambling;

        public override void Install(ModuleManager manager)
        {
            manager.CreateCommands("", cgb =>
            {
                cgb.AddCheck(PermissionChecker.Instance);

                commands.ForEach(com => com.Init(cgb));

                cgb.CreateCommand(Prefix + "raffle")
                    .Description("Prints a name and ID of a random user from the online list from the (optional) role.")
                    .Parameter("role", ParameterType.Optional)
                    .Do(async e =>
                    {
                var arg = string.IsNullOrWhiteSpace(e.GetArg("role")) ? "@everyone" : e.GetArg("role");
                var role = e.Server.FindRoles(arg).FirstOrDefault();
                        if (role == null)
                        {
                    await e.Channel.SendMessage("💢 Role not found.").ConfigureAwait(false);
                            return;
                        }
                var members = role.Members.Where(u => u.Status == UserStatus.Online); // only online
                var membersArray = members as User[] ?? members.ToArray();
                var usr = membersArray[new Random().Next(0, membersArray.Length)];
                await e.Channel.SendMessage($"**Raffled user:** {usr.Name} (id: {usr.Id})").ConfigureAwait(false);
                    });

            cgb.CreateCommand(Prefix + "$$")
                    .Description(string.Format("Check how much {0}s you have.", WizBot.Config.CurrencyName))
                    .Parameter("all", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var usr = e.Message.MentionedUsers.FirstOrDefault() ?? e.User;
                        var pts = GetUserFlowers(usr.Id);
                        var str = $"{usr.Name} has {pts} {WizBot.Config.CurrencySign}";
                        await e.Channel.SendMessage(str).ConfigureAwait(false);
                    });

            cgb.CreateCommand(Prefix + "give")
                    .Description(string.Format("Give someone a certain amount of {0}s", WizBot.Config.CurrencyName))
                    .Parameter("amount", ParameterType.Required)
                    .Parameter("receiver", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var amountStr = e.GetArg("amount")?.Trim();
                        long amount;
                        if (!long.TryParse(amountStr, out amount) || amount < 0)
                            return;

                        var mentionedUser = e.Message.MentionedUsers.FirstOrDefault(u =>
                                                            u.Id != WizBot.Client.CurrentUser.Id &&
                                                            u.Id != e.User.Id);
                        if (mentionedUser == null)
                            return;

                        var userFlowers = GetUserFlowers(e.User.Id);

                        if (userFlowers < amount)
                        {
                            await e.Channel.SendMessage($"{e.User.Mention} You don't have enough {WizBot.Config.CurrencyName}s. You have only {userFlowers}{WizBot.Config.CurrencySign}.").ConfigureAwait(false);
                            return;
                        }

                        FlowersHandler.RemoveFlowers(e.User, "Gift", (int)amount);
                        await FlowersHandler.AddFlowersAsync(mentionedUser, "Gift", (int)amount).ConfigureAwait(false);

                        await e.Channel.SendMessage($"{e.User.Mention} successfully sent {amount} {WizBot.Config.CurrencyName}s to {mentionedUser.Mention}!").ConfigureAwait(false);

                    });

                cgb.CreateCommand(Prefix + "award")
                    .Description("Gives someone a certain amount of flowers. **Owner only!**")
                    .AddCheck(SimpleCheckers.OwnerOnly())
                    .Parameter("amount", ParameterType.Required)
                    .Parameter("receiver", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var amountStr = e.GetArg("amount")?.Trim();
                        long amount;
                        if (!long.TryParse(amountStr, out amount) || amount < 0)
                            return;

                        var mentionedUser = e.Message.MentionedUsers.FirstOrDefault(u =>
                                                            u.Id != WizBot.Client.CurrentUser.Id);
                        if (mentionedUser == null)
                            return;

                        await FlowersHandler.AddFlowersAsync(mentionedUser, $"Awarded by bot owner. ({e.User.Name}/{e.User.Id})", (int)amount).ConfigureAwait(false);

                        await e.Channel.SendMessage($"{e.User.Mention} successfully awarded {amount} {WizBot.Config.CurrencyName}s to {mentionedUser.Mention}!").ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "take")
                    .Description("Takes a certain amount of flowers from someone. **Owner only!**")
                    .AddCheck(SimpleCheckers.OwnerOnly())
                    .Parameter("amount", ParameterType.Required)
                    .Parameter("rektperson", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var amountStr = e.GetArg("amount")?.Trim();
                        long amount;
                        if (!long.TryParse(amountStr, out amount) || amount < 0)
                            return;

                        var mentionedUser = e.Message.MentionedUsers.FirstOrDefault(u =>
                                                            u.Id != WizBot.Client.CurrentUser.Id);
                        if (mentionedUser == null)
                            return;

                        FlowersHandler.RemoveFlowers(mentionedUser, $"Taken by bot owner.({e.User.Name}/{e.User.Id})", (int)amount);

                        await e.Channel.SendMessage($"{e.User.Mention} successfully took {amount} {WizBot.Config.CurrencyName}s from {mentionedUser.Mention}!").ConfigureAwait(false);
                    });

            cgb.CreateCommand(Prefix + "leaderboard")
                                .Alias(Prefix + "lb")
                                .Do(async e =>
                                {
                var richestTemp = DbHandler.Instance.GetTopRichest();
                var richest = richestTemp as CurrencyState[] ?? richestTemp.ToArray();
                        if (richest.Length == 0)
                            return;
                await e.Channel.SendMessage(
                richest.Aggregate(new StringBuilder(
                $@"```xl
┏━━━━━━━━━━━━━━━━━━━┳━━━━━━━┓
┃        Id         ┃  $$$  ┃
"),
                (cur, cs) => cur.AppendLine(
                $@"┣━━━━━━━━━━━━━━━━━━━╋━━━━━━━┫
┃{cs.UserId,-18} ┃ {cs.Value,5} ┃")
                                ).ToString() + "┗━━━━━━━━━━━━━━━━━━━┻━━━━━━━┛```");
                    });
            });
        }

        private static long GetUserFlowers(ulong userId) =>
            Classes.DbHandler.Instance.GetStateByUserId((long)userId)?.Value ?? 0;

    }
}
