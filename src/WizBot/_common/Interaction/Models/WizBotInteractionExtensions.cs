namespace WizBot;

public static class WizBotInteractionExtensions
{
    public static MessageComponent CreateComponent(
        this WizBotInteractionBase wizbotInteractionBase
    )
    {
        var cb = new ComponentBuilder();

        wizbotInteractionBase.AddTo(cb);

        return cb.Build();
    }
}