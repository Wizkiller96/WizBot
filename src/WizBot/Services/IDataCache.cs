using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WizBot.Services
{
    public interface IDataCache
    {
        Task<(bool Success, byte[] Data)> TryGetImageDataAsync(string key);
        Task SetImageDataAsync(string key, byte[] data);
    }
}