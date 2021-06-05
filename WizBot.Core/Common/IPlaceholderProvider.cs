using System;
using System.Collections.Generic;

namespace WizBot.Core.Common
{
    public interface IPlaceholderProvider
    {
        public IEnumerable<(string Name, Func<string> Func)> GetPlaceholders();
    }
}