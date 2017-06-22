namespace WizBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 2 && int.TryParse(args[0], out int shardId) && int.TryParse(args[1], out int parentProcessId))
                new WizBot(shardId, parentProcessId).RunAndBlockAsync(args).GetAwaiter().GetResult();
            else
                new WizBot(0, 0).RunAndBlockAsync(args).GetAwaiter().GetResult();
        }
    }
}