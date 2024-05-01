public class ResponseMessageModel
{
    public IMessageChannel TargetChannel { get; set; }
    public MessageReference MessageReference { get; set; }
    public string Text { get; set; }
    public Embed Embed { get; set; }
    public Embed[] Embeds { get; set; }
    public AllowedMentions SanitizeMentions { get; set; }
    public IUser User { get; set; }
}