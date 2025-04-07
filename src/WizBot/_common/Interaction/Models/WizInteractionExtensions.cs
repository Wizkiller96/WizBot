namespace WizBot;

public static class WizInteractionExtensions
{
    public static MessageComponent CreateComponent(
        this WizInteractionBase wizInteractionBase
    )
    {
        var cb = new ComponentBuilder();

        wizInteractionBase.AddTo(cb);

        return cb.Build();
    }
}