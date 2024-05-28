namespace WizBot.Modules.Xp.Services;

public enum ClubAcceptResult
{
    Accepted,
    NotOwnerOrAdmin,
    NoSuchApplicant,
}

public enum ClubDenyResult
{
    Rejected,
    NoSuchApplicant,
    NotOwnerOrAdmin
}