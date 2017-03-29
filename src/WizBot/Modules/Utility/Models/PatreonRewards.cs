using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WizBot.Services.Database.Models
{
    public class PatreonRewards : DbEntity
    {
        public ulong UserId { get; set; }
        public ulong PledgeCents { get; set; }
        public ulong Awarded { get; set; }
    }
}