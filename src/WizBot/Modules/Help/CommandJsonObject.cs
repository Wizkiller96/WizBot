#nullable disable
namespace WizBot.Modules.Help;

internal class CommandJsonObject
{
    public string[] Aliases { get; set; }
    public string Description { get; set; }
    public string[] Usage { get; set; }
    public string Submodule { get; set; }
    public string Module { get; set; }
    public List<string> Options { get; set; }
    public string[] Requirements { get; set; }
}