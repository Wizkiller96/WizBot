using WizBot.Core.Services;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WizBot
{
    public sealed class Program
    {
        public static Task Main(string[] args)
        {
            if (args.Length == 2
                && int.TryParse(args[0], out int shardId)
                && int.TryParse(args[1], out int parentProcessId))
            {
                return new WizBot(shardId, parentProcessId)
                    .RunAndBlockAsync();
            }
            else
            {
#if DEBUG
                var _ = new WizBot(0, Process.GetCurrentProcess().Id)
                       .RunAsync();
#endif
                return new ShardsCoordinator()
                    .RunAndBlockAsync();
            }
        }
    }
}