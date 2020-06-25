using CommandLine;

namespace WizBot.Core.Common
{
    public class LbOpts : IWizBotCommandOptions
    {
        [Option('c', "clean", Default = false, HelpText = "Only show users who are on the server.")]
        public bool Clean { get; set; }
        public void NormalizeOptions()
        {

        }
    }
}
