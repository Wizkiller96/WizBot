#nullable disable
using NadekoBot.Modules.Games.Services;

namespace NadekoBot.Modules.Games;

/* more games
- Shiritori
- Simple RPG adventure
*/
public partial class Games : NadekoModule<GamesService>
{
    private readonly IImageCache _images;
    private readonly IHttpClientFactory _httpFactory;
    private readonly Random _rng = new();

    public Games(IImageCache images, IHttpClientFactory factory)
    {
        _images = images;
        _httpFactory = factory;
    }

    [Cmd]
    public async Task Choose([Leftover] string list = null)
    {
        if (string.IsNullOrWhiteSpace(list))
            return;
        var listArr = list.Split(';');
        if (listArr.Length < 2)
            return;
        var rng = new NadekoRandom();
        await Response().Confirm("🤔", listArr[rng.Next(0, listArr.Length)]).SendAsync();
    }

    [Cmd]
    public async Task EightBall([Leftover] string question = null)
    {
        if (string.IsNullOrWhiteSpace(question))
            return;

        var res = _service.GetEightballResponse(ctx.User.Id, question);
        await Response().Embed(_sender.CreateEmbed()
            .WithOkColor()
            .WithDescription(ctx.User.ToString())
            .AddField("❓ " + GetText(strs.question), question)
            .AddField("🎱 " + GetText(strs._8ball), res)).SendAsync();
    }
}