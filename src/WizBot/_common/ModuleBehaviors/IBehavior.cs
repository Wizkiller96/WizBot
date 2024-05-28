namespace WizBot.Common.ModuleBehaviors;

public interface IBehavior
{
    public virtual string Name => this.GetType().Name;
}