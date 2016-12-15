namespace WizBot
{
    public class Program
    {
        public static void Main(string[] args) => 
            new WizBot().RunAndBlockAsync(args).GetAwaiter().GetResult();
    }
}
