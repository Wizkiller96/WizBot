public class RobloxUserInfo
{
    public long UserId { get; set; }
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Blurb { get; set; } = "";
    public bool VerifiedBadge { get; set; }

    public string JoinDate { get; set; } = "";
    public int Age { get; set; }

    public int FriendCount { get; set; }
    public int FollowerCount { get; set; }
    public int FollowingCount { get; set; }

    public List<string> OldNames { get; set; } = new();

    public bool IsBanned { get; set; }

    public string Avatar { get; set; } = "";
    public string AvatarBust { get; set; } = "";
    public string AvatarHeadshot { get; set; } = "";
}