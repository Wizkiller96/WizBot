using System.Collections.Generic;

namespace WizBot.Core.Services
{
    /// <summary>
    /// Basic interface used for classes implementing strings loading mechanism
    /// </summary>
    public interface IStringsSource
    {
        /// <summary>
        /// Gets all response strings
        /// </summary>
        /// <returns>Dictionary(localename, Dictionary(key, response))</returns>
        public Dictionary<string, Dictionary<string, string>> GetResponseStrings();
    }
}