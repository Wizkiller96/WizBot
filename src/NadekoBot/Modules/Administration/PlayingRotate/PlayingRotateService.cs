#nullable disable
using Microsoft.EntityFrameworkCore;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Administration.Services;

public sealed class PlayingRotateService : INService, IReadyExecutor
{
    private readonly BotConfigService _bss;
    private readonly SelfService _selfService;
    private readonly Replacer _rep;
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;

    public PlayingRotateService(
        DiscordSocketClient client,
        DbService db,
        BotConfigService bss,
        IEnumerable<IPlaceholderProvider> phProviders,
        SelfService selfService)
    {
        _db = db;
        _bss = bss;
        _selfService = selfService;
        _client = client;

        if (client.ShardId == 0)
            _rep = new ReplacementBuilder().WithClient(client).WithProviders(phProviders).Build();
    }

    public async Task OnReadyAsync()
    {
        if (_client.ShardId != 0)
            return;
        
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        var index = 0;
        while (await timer.WaitForNextTickAsync())
        {
            try
            {
                if (!_bss.Data.RotateStatuses)
                    continue;

                IReadOnlyList<RotatingPlayingStatus> rotatingStatuses;
                await using (var uow = _db.GetDbContext())
                {
                    rotatingStatuses = uow.RotatingStatus.AsNoTracking().OrderBy(x => x.Id).ToList();
                }

                if (rotatingStatuses.Count == 0)
                    continue;

                var playingStatus = index >= rotatingStatuses.Count
                    ? rotatingStatuses[index = 0]
                    : rotatingStatuses[index++];

                var statusText = _rep.Replace(playingStatus.Status);
                await _selfService.SetGameAsync(statusText, playingStatus.Type);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Rotating playing status errored: {ErrorMessage}", ex.Message);
            }
        }
    }

    public async Task<string> RemovePlayingAsync(int index)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index));

        await using var uow = _db.GetDbContext();
        var toRemove = await uow.RotatingStatus.AsQueryable().AsNoTracking().Skip(index).FirstOrDefaultAsync();

        if (toRemove is null)
            return null;

        uow.Remove(toRemove);
        await uow.SaveChangesAsync();
        return toRemove.Status;
    }

    public async Task AddPlaying(ActivityType t, string status)
    {
        await using var uow = _db.GetDbContext();
        var toAdd = new RotatingPlayingStatus
        {
            Status = status,
            Type = t
        };
        uow.Add(toAdd);
        await uow.SaveChangesAsync();
    }

    public bool ToggleRotatePlaying()
    {
        var enabled = false;
        _bss.ModifyConfig(bs => { enabled = bs.RotateStatuses = !bs.RotateStatuses; });
        return enabled;
    }

    public IReadOnlyList<RotatingPlayingStatus> GetRotatingStatuses()
    {
        using var uow = _db.GetDbContext();
        return uow.RotatingStatus.AsNoTracking().ToList();
    }
}