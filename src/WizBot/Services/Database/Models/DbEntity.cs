﻿using System;
using System.ComponentModel.DataAnnotations;

namespace WizBot.Services.Database.Models
{
    public class DbEntity
    {
        [Key]
        public int Id { get; set; }
        public DateTime DateAdded { get; } = DateTime.UtcNow;
    }
}
