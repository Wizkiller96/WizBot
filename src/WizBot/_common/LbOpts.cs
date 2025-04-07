#nullable disable
using CommandLine;

namespace WizBot.Common;

public class LbOpts : IWizCommandOptions
{
    [Option('c', "clean", Default = false, HelpText = "Only show users who are on the server.")]
    public bool Clean { get; set; }

    public void NormalizeOptions()
    {
    }
}