using System;
using System.Collections.Generic;

namespace NadekoBot.Common
{
    public interface IPlaceholderProvider
    {
        public IEnumerable<(string Name, Func<string> Func)> GetPlaceholders();
    }
}