using WizBot.Common.ModuleBehaviors;

namespace WizBot.Modules.Utility;

public sealed class AfkService : INService, IReadyExecutor
{
    private readonly IBotCache _cache;
    private readonly DiscordSocketClient _client;
    private readonly MessageSenderService _mss;

    private static readonly TimeSpan _maxAfkDuration = 8.Hours();
    public AfkService(IBotCache cache, DiscordSocketClient client, MessageSenderService mss)
    {
        _cache = cache;
        _client = client;
        _mss = mss;
    }

    private static TypedKey<string> GetKey(ulong userId)
        => new($"afk:msg:{userId}");

    public async Task<bool> SetAfkAsync(ulong userId, string text)
    {
        var added = await _cache.AddAsync(GetKey(userId), text, _maxAfkDuration, overwrite: true);

        async Task StopAfk(SocketMessage socketMessage)
        {
            try
            {
                if (socketMessage.Author?.Id == userId)
                {
                    await _cache.RemoveAsync(GetKey(userId));
                    _client.MessageReceived -= StopAfk;

                    // write the message saying afk status cleared

                    if (socketMessage.Channel is ITextChannel tc)
                    {
                        _ = Task.Run(async () =>
                        {
                            var msg = await _mss.Response(tc).Confirm("AFK message cleared!").SendAsync();

                            msg.DeleteAfter(5);
                        });
                    }

                }

            }
            catch (Exception ex)
            {
                Log.Warning("Unexpected error occurred while trying to stop afk: {Message}", ex.Message);
            }
        }

        _client.MessageReceived += StopAfk;


        _ = Task.Run(async () =>
        {
            await Task.Delay(_maxAfkDuration);
            _client.MessageReceived -= StopAfk;
        });
        
        return added;
    }

    public Task OnReadyAsync()
    {
        _client.MessageReceived += TryTriggerAfkMessage;

        return Task.CompletedTask;
    }

    private Task TryTriggerAfkMessage(SocketMessage arg)
    {
        if (arg.Author.IsBot)
            return Task.CompletedTask;

        if (arg is not IUserMessage uMsg || uMsg.Channel is not ITextChannel tc)
            return Task.CompletedTask;
        
        if ((arg.MentionedUsers.Count is 0 or > 3) && uMsg.ReferencedMessage is null)
            return Task.CompletedTask;

        _ = Task.Run(async () =>
        {
            var botUser = await tc.Guild.GetCurrentUserAsync();

            var perms = botUser.GetPermissions(tc);

            if (!perms.SendMessages)
                return;

            ulong mentionedUserId = 0;

            if (arg.MentionedUsers.Count <= 3)
            {
                foreach (var uid in uMsg.MentionedUserIds)
                {
                    if (uid == arg.Author.Id)
                        continue;

                    if (arg.Content.StartsWith($"<@{uid}>") || arg.Content.StartsWith($"<@!{uid}>"))
                    {
                        mentionedUserId = uid;
                        break;
                    }
                }
            }

            if (mentionedUserId == 0)
            {
                if (uMsg.ReferencedMessage?.Author?.Id is not ulong repliedUserId)
                {
                    return;
                }
                
                mentionedUserId = repliedUserId;
            }

            try
            {
                var result = await _cache.GetAsync(GetKey(mentionedUserId));
                if (result.TryPickT0(out var msg, out _))
                {
                    var st = SmartText.CreateFrom(msg);
                    
                    st = "The user is AFK: " + st;
                    
                    var toDelete = await _mss.Response(arg.Channel)
                                             .Message(uMsg)
                                             .Text(st)
                                             .Sanitize(false)
                                             .SendAsync();

                    toDelete.DeleteAfter(30);
                }
            }
            catch (HttpException ex)
            {
                Log.Warning("Error in afk service: {Message}", ex.Message);
            }
        });

        return Task.CompletedTask;
    }
}