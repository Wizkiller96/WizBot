#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WizBot.Db;
using WizBot.Db.Models;

namespace WizBot.Modules.Xp.Services;

public class ClubService : INService, IClubService
{
    private readonly DbService _db;
    private readonly IHttpClientFactory _httpFactory;

    public ClubService(DbService db, IHttpClientFactory httpFactory)
    {
        _db = db;
        _httpFactory = httpFactory;
    }

    public enum CLubCreateResult
    {
        Success,
        AlreadyInAClub,
        NameTaken,
        InsufficientLevel,
    }
    
    public async Task<bool?> CreateClubAsync(IUser user, string clubName)
    {
        //must be lvl 5 and must not be in a club already

        await using var uow = _db.GetDbContext();
        var du = uow.GetOrCreateUser(user);
        var xp = new LevelStats(du.TotalXp);
        
        if (xp.Level < 5 || du.ClubId is not null)
            return false;

        if (await uow.Clubs.AnyAsyncEF(x => x.Name == clubName))
            return null;
        
        du.IsClubAdmin = true;
        du.Club = new()
        {
            Name = clubName,
            Owner = du
        };
        uow.Clubs.Add(du.Club);
        await uow.SaveChangesAsync();

        await uow.GetTable<ClubApplicants>()
                 .DeleteAsync(x => x.UserId == du.Id);

        return true;
    }

    public enum ClubTransferError
    {
        NotOwner,
        TargetNotMember
    }
    
    public ClubInfo TransferClub(IUser from, IUser newOwner)
    {
        using var uow = _db.GetDbContext();
        var club = uow.Clubs.GetByOwner(@from.Id);
        var newOwnerUser = uow.GetOrCreateUser(newOwner);

        if (club is null || club.Owner.UserId != from.Id || !club.Members.Contains(newOwnerUser))
            return null;

        club.Owner.IsClubAdmin = true; // old owner will stay as admin
        newOwnerUser.IsClubAdmin = true;
        club.Owner = newOwnerUser;
        uow.SaveChanges();
        return club;
    }

    public enum ToggleAdminResult
    {
        AddedAdmin,
        RemovedAdmin,
        TargetNotMember,
        CanTargetThyself,
    }
    
    public async Task<bool?> ToggleAdminAsync(IUser owner, IUser toAdmin)
    {
        await using var uow = _db.GetDbContext();
        var club = uow.Clubs.GetByOwner(owner.Id);
        var adminUser = uow.GetOrCreateUser(toAdmin);

        if (club is null || club.Owner.UserId != owner.Id || !club.Members.Contains(adminUser))
            return null;

        if (club.OwnerId == adminUser.Id)
            return true;

        var newState = adminUser.IsClubAdmin = !adminUser.IsClubAdmin;
        await uow.SaveChangesAsync();
        return newState;
    }

    public ClubInfo GetClubByMember(IUser user)
    {
        using var uow = _db.GetDbContext();
        var member = uow.Clubs.GetByMember(user.Id);
        return member;
    }

    public enum SetClubIconResult
    {
        Success,
        InvalidFiletype,
        TooLarge,
        NotOwner,
    }
    
    public async Task<bool> SetClubIconAsync(ulong ownerUserId, string url)
    {
        if (url is not null)
        {
            using var http = _httpFactory.CreateClient();
            using var temp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!temp.IsImage() || temp.GetContentLength() > 5.Megabytes().Bytes)
                return false;
        }

        await using var uow = _db.GetDbContext();
        var club = uow.Clubs.GetByOwner(ownerUserId);

        if (club is null)
            return false;

        club.ImageUrl = url;
        await uow.SaveChangesAsync();

        return true;
    }

    public bool GetClubByName(string clubName, out ClubInfo club)
    {
        using var uow = _db.GetDbContext();
        club = uow.Clubs.GetByName(clubName);

        return club is not null;
    }

    public ClubApplyResult ApplyToClub(IUser user, ClubInfo club)
    {
        using var uow = _db.GetDbContext();
        var du = uow.GetOrCreateUser(user);
        uow.SaveChanges();

        //user banned or a member of a club, or already applied,
        // or doesn't min minumum level requirement, can't apply
        if (du.Club is not null)
            return ClubApplyResult.AlreadyInAClub;
        
        if (club.Bans.Any(x => x.UserId == du.Id))
            return ClubApplyResult.Banned;
        
        if (club.Applicants.Any(x => x.UserId == du.Id))
            return ClubApplyResult.InsufficientLevel;

        var app = new ClubApplicants
        {
            ClubId = club.Id,
            UserId = du.Id
        };

        uow.Set<ClubApplicants>().Add(app);
        uow.SaveChanges();
        return ClubApplyResult.Success;
    }

    public enum ClubAcceptResult
    {
        Accepted,
        NotOwnerOrAdmin,
        NoSuchApplicant,
    }
    
    public bool AcceptApplication(ulong clubOwnerUserId, string userName, out DiscordUser discordUser)
    {
        discordUser = null;
        using var uow = _db.GetDbContext();
        var club = uow.Clubs.GetByOwnerOrAdmin(clubOwnerUserId);
        if (club is null)
            return false;

        var applicant =
            club.Applicants.FirstOrDefault(x => x.User.ToString().ToUpperInvariant() == userName.ToUpperInvariant());
        if (applicant is null)
            return false;

        applicant.User.Club = club;
        applicant.User.IsClubAdmin = false;
        club.Applicants.Remove(applicant);

        //remove that user's all other applications
        uow.Set<ClubApplicants>()
           .RemoveRange(uow.Set<ClubApplicants>().AsQueryable().Where(x => x.UserId == applicant.User.Id));

        discordUser = applicant.User;
        uow.SaveChanges();
        return true;
    }

    public ClubInfo GetClubWithBansAndApplications(ulong ownerUserId)
    {
        using var uow = _db.GetDbContext();
        return uow.Clubs.GetByOwnerOrAdmin(ownerUserId);
    }

    public ClubLeaveResult LeaveClub(IUser user)
    {
        using var uow = _db.GetDbContext();
        var du = uow.GetOrCreateUser(user, x => x.Include(u => u.Club));
        if (du.Club is null)
            return ClubLeaveResult.NotInAClub; 
        if (du.Club.OwnerId == du.Id)
            return ClubLeaveResult.OwnerCantLeave;

        du.Club = null;
        du.IsClubAdmin = false;
        uow.SaveChanges();
        return ClubLeaveResult.Success;
    }

    public bool SetDescription(ulong userId, string desc)
    {
        using var uow = _db.GetDbContext();
        var club = uow.Clubs.GetByOwner(userId);
        if (club is null)
            return false;

        club.Description = desc?.TrimTo(150, true);
        uow.SaveChanges();

        return true;
    }

    public bool Disband(ulong userId, out ClubInfo club)
    {
        using var uow = _db.GetDbContext();
        club = uow.Clubs.GetByOwner(userId);
        if (club is null)
            return false;

        uow.Clubs.Remove(club);
        uow.SaveChanges();
        return true;
    }

    public ClubBanResult Ban(ulong bannerId, string userName, out ClubInfo club)
    {
        using var uow = _db.GetDbContext();
        club = uow.Clubs.GetByOwnerOrAdmin(bannerId);
        if (club is null)
            return ClubBanResult.NotOwnerOrAdmin;

        var usr = club.Members.FirstOrDefault(x => x.ToString().ToUpperInvariant() == userName.ToUpperInvariant())
                  ?? club.Applicants
                         .FirstOrDefault(x => x.User.ToString().ToUpperInvariant() == userName.ToUpperInvariant())
                         ?.User;
        if (usr is null)
            return ClubBanResult.WrongUser;

        if (club.OwnerId == usr.Id
            || (usr.IsClubAdmin && club.Owner.UserId != bannerId)) // can't ban the owner kek, whew
            return ClubBanResult.Unbannable;

        club.Bans.Add(new()
        {
            Club = club,
            User = usr
        });
        club.Members.Remove(usr);

        var app = club.Applicants.FirstOrDefault(x => x.UserId == usr.Id);
        if (app is not null)
            club.Applicants.Remove(app);

        uow.SaveChanges();

        return ClubBanResult.Success;
    }

    public ClubUnbanResult UnBan(ulong ownerUserId, string userName, out ClubInfo club)
    {
        using var uow = _db.GetDbContext();
        club = uow.Clubs.GetByOwnerOrAdmin(ownerUserId);
        if (club is null)
            return ClubUnbanResult.NotOwnerOrAdmin;

        var ban = club.Bans.FirstOrDefault(x => x.User.ToString().ToUpperInvariant() == userName.ToUpperInvariant());
        if (ban is null)
            return ClubUnbanResult.WrongUser;

        club.Bans.Remove(ban);
        uow.SaveChanges();

        return ClubUnbanResult.Success;
    }

    public enum ClubKickResult
    {
        Success,
        NotOwnerOrAdmin,
        TargetNotAMember,
        Hierarchy
    }
    
    public bool Kick(ulong kickerId, string userName, out ClubInfo club)
    {
        using var uow = _db.GetDbContext();
        club = uow.Clubs.GetByOwnerOrAdmin(kickerId);
        if (club is null)
            return false;

        var usr = club.Members.FirstOrDefault(x => x.ToString().ToUpperInvariant() == userName.ToUpperInvariant());
        if (usr is null)
            return false;

        if (club.OwnerId == usr.Id || (usr.IsClubAdmin && club.Owner.UserId != kickerId))
            return false;

        club.Members.Remove(usr);
        var app = club.Applicants.FirstOrDefault(x => x.UserId == usr.Id);
        if (app is not null)
            club.Applicants.Remove(app);
        uow.SaveChanges();

        return true;
    }

    public List<ClubInfo> GetClubLeaderboardPage(int page)
    {
        if (page < 0)
            throw new ArgumentOutOfRangeException(nameof(page));

        using var uow = _db.GetDbContext();
        return uow.Clubs.GetClubLeaderboardPage(page);
    }
}