#nullable disable
using NadekoBot.Modules.Xp.Services;

namespace NadekoBot.Modules.Xp;

public class LevelStats
{
    public long Level { get; }
    public long LevelXp { get; }
    public long RequiredXp { get; }
    public long TotalXp { get; }

    public LevelStats(long xp)
    {
        if (xp < 0)
            xp = 0;

        TotalXp = xp;

        const int baseXp = XpService.XP_REQUIRED_LVL_1;

        var required = baseXp;
        var totalXp = 0;
        var lvl = 1;
        while (true)
        {
            required = (int)(baseXp + (baseXp / 4.0 * (lvl - 1)));

            if (required + totalXp > xp)
                break;

            totalXp += required;
            lvl++;
        }

        Level = lvl - 1;
        LevelXp = xp - totalXp;
        RequiredXp = required;
    }
}