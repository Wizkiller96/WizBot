//using Discord.Commands;
//using WizBot.Common.Attributes;
//using WizBot.Modules.Administration.Services;
//using WizBot.Extensions;
//using System;
//using System.IO;
//using System.Reflection;
//using System.Text.RegularExpressions;
//using System.Threading.Tasks;
//using System.Linq;

//namespace WizBot.Modules.Administration
//{
//    public partial class Administration
//    {
//        [Group]
//        public class PackagesCommands : WizBotSubmodule<PackagesService>
//        {
//            private readonly WizBot _bot;

//            public PackagesCommands(WizBot bot)
//            {
//                _bot = bot;
//            }

//            [WizBotCommand, Usage, Description, Aliases]
//            [RequireContext(ContextType.Guild)]
//            public async Task PackageList()
//            {
//                _service.ReloadAvailablePackages();
//                await Context.Channel.SendConfirmAsync(
//                    string.Join(
//                        "\n",
//                        _service.Packages
//                            .Select(x => _bot.LoadedPackages.Contains(x)
//                                ? "【✘】" + x
//                                : "【  】" + x)));
//            }

//            [WizBotCommand, Usage, Description, Aliases]
//            [RequireContext(ContextType.Guild)]
//            [OwnerOnly]
//            public async Task PackageUnload(string name)
//            {
//                if (name.Contains(":") || name.Contains(".") || name.Contains("\\") || name.Contains("/") || name.Contains("~"))
//                    return;
//                name = name.ToTitleCase();
//                var package = Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory,
//                                                "modules",
//                                                $"WizBot.Modules.{name}",
//                                                $"WizBot.Modules.{name}.dll"));

//                await _bot.UnloadPackage(name).ConfigureAwait(false);
//                await ReplyAsync(":ok:");
//            }

//            [WizBotCommand, Usage, Description, Aliases]
//            [RequireContext(ContextType.Guild)]
//            [OwnerOnly]
//            public async Task PackageLoad(string name)
//            {
//                if (name.Contains(".") || name.Contains("\\") || name.Contains("/") || name.Contains("~"))
//                    return;
//                name = name.ToTitleCase();

//                if (await _bot.LoadPackage(name))
//                    await ReplyAsync(":ok:");
//                else
//                    await ReplyAsync(":x:");
//            }

//            [WizBotCommand, Usage, Description, Aliases]
//            [RequireContext(ContextType.Guild)]
//            [OwnerOnly]
//            public async Task PackageReload(string name)
//            {
//                if (name.Contains(".") || name.Contains("\\") || name.Contains("/") || name.Contains("~"))
//                    return;
//                name = name.ToTitleCase();

//                if (await _bot.UnloadPackage(name))
//                {
//                    await _bot.LoadPackage(name);
//                    await ReplyAsync(":ok:");
//                }
//                else
//                    await ReplyAsync(":x:");
//            }
//        }
//    }
//}
