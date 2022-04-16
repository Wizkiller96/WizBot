using Microsoft.Extensions.DependencyInjection;

public sealed class DmContextAdapter : DmContext
{
    public override IMedusaStrings Strings { get; }
    public override IDMChannel Channel { get; }
    public override IUserMessage Message { get; }
    public override IUser User
        => Message.Author;
    
    private readonly IServiceProvider _services;
    private readonly Lazy<IEmbedBuilderService> _ebs;
    private readonly Lazy<IBotStrings> _botStrings;
    private readonly Lazy<ILocalization> _localization;

    public DmContextAdapter(ICommandContext ctx, IMedusaStrings strings, IServiceProvider services)
    {
        if (ctx is not { Channel: IDMChannel ch })
        {
            throw new ArgumentException("Can't use non-dm context to create DmContextAdapter", nameof(ctx));
        }

        Strings = strings;

        _services = services;

        Channel = ch;
        Message = ctx.Message;
        
        
        _ebs = new(_services.GetRequiredService<IEmbedBuilderService>());
        _botStrings = new(_services.GetRequiredService<IBotStrings>);
        _localization = new(_services.GetRequiredService<ILocalization>());
    }

    public override IEmbedBuilder Embed()
        => _ebs.Value.Create();

    public override string GetText(string key, object[]? args = null)
    {
        var cultureInfo = _localization.Value.GetCultureInfo(default(ulong?));
        var output = Strings.GetText(key, cultureInfo, args ?? Array.Empty<object>());
        if (!string.IsNullOrWhiteSpace(output))
            return output;
        
        return _botStrings.Value.GetText(key, cultureInfo, args);
    }
}