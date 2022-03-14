#nullable disable
namespace WizBot.Modules.Gambling;

public class SlotResponse
{
    public float Multiplier { get; set; }
    public long Won { get; set; }
    public List<int> Rolls { get; set; } = new();
    public GamblingError Error { get; set; }
}