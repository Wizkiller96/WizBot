using WizBot.Db.Models;

namespace WizBot.Modules.Xp.Services;

public interface IClubService
{
    Task<bool?> CreateClubAsync(IUser user, string clubName);
    ClubInfo? TransferClub(IUser from, IUser newOwner);
    Task<bool?> ToggleAdminAsync(IUser owner, IUser toAdmin);
    ClubInfo? GetClubByMember(IUser user);
    Task<bool> SetClubIconAsync(ulong ownerUserId, string? url);
    bool GetClubByName(string clubName, out ClubInfo club);
    ClubApplyResult ApplyToClub(IUser user, ClubInfo club);
    bool AcceptApplication(ulong clubOwnerUserId, string userName, out DiscordUser discordUser);
    ClubInfo? GetClubWithBansAndApplications(ulong ownerUserId);
    ClubLeaveResult LeaveClub(IUser user);
    bool SetDescription(ulong userId, string? desc);
    bool Disband(ulong userId, out ClubInfo club);
    ClubBanResult Ban(ulong bannerId, string userName, out ClubInfo club);
    ClubUnbanResult UnBan(ulong ownerUserId, string userName, out ClubInfo club);
    bool Kick(ulong kickerId, string userName, out ClubInfo club);
    List<ClubInfo> GetClubLeaderboardPage(int page);
}

public enum ClubApplyResult
{
    Success,

    AlreadyInAClub,
    Banned,
    InsufficientLevel
}