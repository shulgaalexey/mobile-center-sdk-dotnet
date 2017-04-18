﻿namespace Microsoft.Azure.Mobile.Utils
{
    /// <summary>
    /// Represents a store of persistent application settings that is behaves like a dictionary.
    /// </summary>
    public interface IApplicationSettings
    {
        // Returns the object corresponding to 'key'. If there is no such object, it creates one with the given default value, and returns that
        T GetValue<T>(string key, T defaultValue);
        void Remove(string key);
        object this[string key] { get; set; }
    }
}
