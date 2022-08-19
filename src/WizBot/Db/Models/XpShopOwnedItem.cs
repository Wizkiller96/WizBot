﻿#nullable disable warnings
using WizBot.Services.Database.Models;

namespace WizBot.Db.Models;

public class XpShopOwnedItem : DbEntity
{
    public ulong UserId { get; set; }
    public XpShopItemType ItemType { get; set; }
    public bool IsUsing { get; set; }
    public string ItemKey { get; set; }
}

public enum XpShopItemType
{
    Background = 0,
    Frame = 1,
}