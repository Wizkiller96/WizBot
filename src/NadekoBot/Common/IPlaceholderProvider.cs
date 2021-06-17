using System;
using System.Collections.Generic;

namespace NadekoBot.Core.Common
{
    public interface IPlaceholderProvider
    {
        public IEnumerable<(string Name, Func<string> Func)> GetPlaceholders();
    }
}