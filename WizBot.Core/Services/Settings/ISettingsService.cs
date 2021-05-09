using System.Collections.Generic;

namespace WizBot.Core.Services
{
    public interface ISettingsService
    {
        public string Name { get; }
        /// <summary>
        /// Loads new data and publishes the new state
        /// </summary>
        void Reload();
        /// <summary>
        /// Gets the list of props you can set
        /// </summary>
        /// <returns>List of props</returns>
        IReadOnlyList<string> GetSettableProps();
        /// <summary>
        /// Gets the value of the specified property
        /// </summary>
        /// <param name="prop">Prop name</param>
        /// <returns>Value of the prop</returns>
        string GetSetting(string prop);

        /// <summary>
        /// Gets the value of the specified property
        /// </summary>
        /// <param name="prop">Prop name</param>
        /// <returns>Value of the prop</returns>
        string GetComment(string prop);

        /// <summary>
        /// Sets the value of the specified property
        /// </summary>
        /// <param name="prop">Property to set</param>
        /// <param name="newValue">Value to set the property to</param>
        /// <returns>Success</returns>
        bool SetSetting(string prop, string newValue);
    }
}