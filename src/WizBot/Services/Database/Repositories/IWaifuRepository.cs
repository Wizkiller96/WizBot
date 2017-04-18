using WizBot.Services.Database.Models;
using System;
using System.Collections.Generic;

namespace WizBot.Services.Database.Repositories
{
    public interface IWaifuRepository : IRepository<WaifuInfo>
    {
        IList<WaifuInfo> GetTop(int count);
        WaifuInfo ByWaifuUserId(ulong userId);
        IList<WaifuInfo> ByClaimerUserId(ulong userId);
    }
}
