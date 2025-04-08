/*
   RegistryHelper
   Copyright (C) 2025 Geir Gustavsen, ZeroLinez Softworx

   This program is free software: you can redistribute it and/or modify
   it under the terms of the GNU General Public License as published by
   the Free Software Foundation, either version 3 of the License, or
   (at your option) any later version.

   This program is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
   GNU General Public License for more details.

   You should have received a copy of the GNU General Public License
   along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using Microsoft.Win32;
using QuickLog;
using System.Security;
using static ZLS.RegistryHelper.Helpers;

namespace ZLS.RegistryHelper
{
    public static class RegistryUtilities
    {
        /// <summary>
        /// Logger instance used for recording registry operations and exceptions.
        /// Defaults to the application's default logger instance.
        /// </summary>
        public static IQuickLog Log { get; set; } = LogManager.GetDefaultLogger();

        private static readonly Dictionary<string, object> RegistryCache = new();

        /// <summary>
        /// Reads a value from the Windows registry with caching support, returning the value cast to type <typeparamref name="T"/>.
        /// If the value does not exist or an error occurs, returns <paramref name="defaultValue"/>.
        /// </summary>
        public static T Read<T>(RegistryHive hive, string subKey, string name, T defaultValue)
        {
            var cacheKey = $"{hive}:{subKey}:{name}";
            if (RegistryCache.TryGetValue(cacheKey, out var cachedValue) && cachedValue is T cachedTypedValue)
                return cachedTypedValue;

            try
            {
                using var key = RegistryKey.OpenBaseKey(hive, Is64Bit ? RegistryView.Registry64 : RegistryView.Registry32);
                using var subKeyHandle = key.OpenSubKey(subKey);
                if (subKeyHandle != null)
                {
                    var value = subKeyHandle.GetValue(name);
                    if (value != null && value is T typedValue)
                    {
                        RegistryCache[cacheKey] = typedValue;
                        return typedValue;
                    }
                }
            }
            catch (SecurityException secEx)
            {
                Log.Log(LogType.Warn, $"Security exception: {secEx.Message}");
            }
            catch (Exception ex)
            {
                Log.Log(LogType.Error, ex);
            }
            return defaultValue;
        }

        /// <summary>
        /// Writes a value to the Windows registry, clears related cache entry, and manages permission issues.
        /// If successful, returns the written value; otherwise, returns default of type <typeparamref name="T"/>.
        /// </summary>
        public static T Write<T>(RegistryHive hive, string subKey, string name, T value)
        {
            var cacheKey = $"{hive}:{subKey}:{name}";
            try
            {
                using var key = RegistryKey.OpenBaseKey(hive, Is64Bit ? RegistryView.Registry64 : RegistryView.Registry32);
                using var subKeyHandle = key.CreateSubKey(subKey, true);
                if (subKeyHandle != null)
                {
                    subKeyHandle.SetValue(name, value);
                    RegistryCache[cacheKey] = value;
                    return value;
                }
            }
            catch (UnauthorizedAccessException authEx)
            {
                Log.Log(LogType.Warn, $"Unauthorized access: {authEx.Message}");
            }
            catch (Exception ex)
            {
                Log.Log(LogType.Error, ex);
            }
            return default;
        }

        /// <summary>
        /// Deletes a value from the Windows registry, clears related cache entry, and handles permission issues.
        /// Returns <c>true</c> if deletion is successful, otherwise <c>false</c>.
        /// </summary>
        public static bool Delete(RegistryHive hive, string subKey, string name)
        {
            var cacheKey = $"{hive}:{subKey}:{name}";
            try
            {
                using var key = RegistryKey.OpenBaseKey(hive, Is64Bit ? RegistryView.Registry64 : RegistryView.Registry32);
                using var subKeyHandle = key.OpenSubKey(subKey, true);
                if (subKeyHandle != null)
                {
                    subKeyHandle.DeleteValue(name);
                    RegistryCache.Remove(cacheKey);
                    return true;
                }
            }
            catch (UnauthorizedAccessException authEx)
            {
                Log.Log(LogType.Warn, $"Unauthorized access: {authEx.Message}");
            }
            catch (Exception ex)
            {
                Log.Log(LogType.Error, ex);
            }
            return false;
        }

        /// <summary>
        /// Checks if a specific registry key exists.
        /// </summary>
        public static bool KeyExists(RegistryHive hive, string subKey)
        {
            try
            {
                using var key = RegistryKey.OpenBaseKey(hive, Is64Bit ? RegistryView.Registry64 : RegistryView.Registry32);
                return key.OpenSubKey(subKey) != null;
            }
            catch (Exception ex)
            {
                Log.Log(LogType.Error, ex);
            }
            return false;
        }

        /// <summary>
        /// Checks if a specific registry value exists.
        /// </summary>
        public static bool ValueExists(RegistryHive hive, string subKey, string name)
        {
            try
            {
                using var key = RegistryKey.OpenBaseKey(hive, Is64Bit ? RegistryView.Registry64 : RegistryView.Registry32);
                using var subKeyHandle = key.OpenSubKey(subKey);
                return subKeyHandle?.GetValue(name) != null;
            }
            catch (Exception ex)
            {
                Log.Log(LogType.Error, ex);
            }
            return false;
        }

        /// <summary>
        /// Deletes an entire registry key and all its subkeys.
        /// </summary>
        public static bool DeleteKey(RegistryHive hive, string subKey)
        {
            try
            {
                using var key = RegistryKey.OpenBaseKey(hive, Is64Bit ? RegistryView.Registry64 : RegistryView.Registry32);
                key.DeleteSubKeyTree(subKey, false);
                RegistryCache.Keys.Where(k => k.StartsWith($"{hive}:{subKey}")).ToList().ForEach(k => RegistryCache.Remove(k));
                return true;
            }
            catch (UnauthorizedAccessException authEx)
            {
                Log.Log(LogType.Warn, $"Unauthorized access: {authEx.Message}");
            }
            catch (Exception ex)
            {
                Log.Log(LogType.Error, ex);
            }
            return false;
        }

        /// <summary>
        /// Enumerates all value names within a specified registry key.
        /// </summary>
        public static IEnumerable<string> EnumerateValues(RegistryHive hive, string subKey)
        {
            try
            {
                using var key = RegistryKey.OpenBaseKey(hive, Is64Bit ? RegistryView.Registry64 : RegistryView.Registry32);
                using var subKeyHandle = key.OpenSubKey(subKey);
                return subKeyHandle?.GetValueNames() ?? Array.Empty<string>();
            }
            catch (Exception ex)
            {
                Log.Log(LogType.Error, ex);
            }
            return Array.Empty<string>();
        }

        /// <summary>
        /// Enumerates all subkey names under a specified registry key.
        /// </summary>
        public static IEnumerable<string> EnumerateSubKeys(RegistryHive hive, string subKey)
        {
            try
            {
                using var key = RegistryKey.OpenBaseKey(hive, Is64Bit ? RegistryView.Registry64 : RegistryView.Registry32);
                using var subKeyHandle = key.OpenSubKey(subKey);
                return subKeyHandle?.GetSubKeyNames() ?? Array.Empty<string>();
            }
            catch (Exception ex)
            {
                Log.Log(LogType.Error, ex);
            }
            return Array.Empty<string>();
        }
    }

}
