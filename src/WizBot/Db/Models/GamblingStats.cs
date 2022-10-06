﻿#nullable disable
namespace WizBot.Services.Database.Models;

public class GamblingStats : DbEntity
{
    public string Feature { get; set; }
    public decimal Bet { get; set; }
    public decimal PaidOut { get; set; }
}