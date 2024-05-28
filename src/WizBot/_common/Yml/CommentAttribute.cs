#nullable disable
namespace WizBot.Common.Yml;

[AttributeUsage(AttributeTargets.Property)]
public class CommentAttribute : Attribute
{
    public string Comment { get; }

    public CommentAttribute(string comment)
        => Comment = comment;
}