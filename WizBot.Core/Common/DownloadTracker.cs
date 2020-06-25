using WizBot.Core.Services;
using System;
using System.Collections.Concurrent;

namespace WizBot.Core.Common
{
    public class DownloadTracker : INService
    {
        public ConcurrentDictionary<ulong, DateTime> LastDownloads { get; } = new ConcurrentDictionary<ulong, DateTime>();
    }
}
