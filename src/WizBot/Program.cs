﻿using WizBot.Core.Services;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WizBot
{
    public sealed class Program
    {
        public static async Task Main(string[] args)
        {
            var pid = Process.GetCurrentProcess().Id;
            System.Console.WriteLine($"Pid: {pid}");
            if (args.Length == 2
                && int.TryParse(args[0], out int shardId)
                && int.TryParse(args[1], out int parentProcessId))
            {
                await new WizBot(shardId, parentProcessId == 0 ? pid : parentProcessId)
                    .RunAndBlockAsync();
            }
            else
            {
                await new ShardsCoordinator()
                    .RunAsync()
                    .ConfigureAwait(false);
#if DEBUG
                await new WizBot(0, pid)
                    .RunAndBlockAsync();
#else
                await Task.Delay(-1);
#endif
            }
        }
    }
}