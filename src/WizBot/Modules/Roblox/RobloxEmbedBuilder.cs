namespace WizBot.Modules.Roblox;

public static class RobloxEmbedBuilder
{
    public static EmbedBuilder BuildUserEmbed(RobloxUserInfo info)
    {
        var pastNames = info.OldNames?.Take(5).ToList();
        var pastNamesText = (pastNames == null || pastNames.Count == 0)
            ? "N/A"
            : string.Join("\n", pastNames);
        
        string joinDateDisplay;

        if (DateTime.TryParse(info.JoinDate, out var parsedDate))
        {
            var unix = ((DateTimeOffset)parsedDate).ToUnixTimeSeconds();
            joinDateDisplay = $"<t:{unix}:d>";
        }
        else
        {
            joinDateDisplay = info.JoinDate;
        }

        return new EmbedBuilder()
            .WithOkColor()
            .WithAuthor(
                $"{info.Username}'s Roblox Info",
                "https://i.imgur.com/jDcWXPD.png",
                $"https://www.roblox.com/users/{info.UserId}/profile"
            )
            .WithThumbnailUrl(info.AvatarBust)
            .AddField("Username", $"[{info.Username}](https://www.roblox.com/users/{info.UserId}/profile)", true)
            .AddField("Display Name", info.DisplayName, true)
            .AddField("User ID", info.UserId.ToString(), true)
            .AddField("Verified Badge", info.VerifiedBadge, true)
            .AddField("Account Age", info.Age.ToString(), true)
            .AddField("Join Date", joinDateDisplay, true)
            .AddField("Friends", info.FriendCount.ToString(), true)
            .AddField("Followers", info.FollowerCount.ToString(), true)
            .AddField("Following", info.FollowingCount.ToString(), true)
            .AddField("Blurb",
                string.IsNullOrWhiteSpace(info.Blurb)
                    ? "N/A"
                    : info.Blurb.TrimTo(170))
            .AddField($"Past Names ({info.OldNames?.Count ?? 0})", pastNamesText);
    }
}