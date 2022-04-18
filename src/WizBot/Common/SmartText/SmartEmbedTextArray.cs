#nullable disable
namespace WizBot;

public sealed record SmartEmbedTextArray : SmartText
{
    public string PlainText { get; set; }
    public SmartEmbedText[] Embeds { get; set; }

    public bool IsValid
        => Embeds?.All(x => x.IsValid) ?? false;

    public EmbedBuilder[] GetEmbedBuilders()
    {
        if (Embeds is null)
            return Array.Empty<EmbedBuilder>();

        return Embeds.Map(em => em.GetEmbed());
    }

    public void NormalizeFields()
    {
        if (Embeds is null)
            return;
        
        foreach(var eb in Embeds)
            eb.NormalizeFields();
    }
}