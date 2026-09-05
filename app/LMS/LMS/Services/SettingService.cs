using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using LMS.Data;
using LMS.Models;

namespace LMS.Services
{
    public class SettingService
    {
        private static readonly Dictionary<string, string> _cache = new();
        private static readonly object _lock = new();

        public static void ReloadCache()
        {
            lock (_lock)
            {
                _cache.Clear();
                try
                {
                    using var db = new AppDbContext();
                    var settings = db.AppSettings.AsNoTracking().ToList();
                    foreach (var s in settings)
                    {
                        _cache[s.SettingKey] = s.SettingValue;
                    }
                }
                catch
                {
                    // Fallback
                }
            }
        }

        public string GetSettingValue(string key, string defaultValue = "")
        {
            return Get(key, defaultValue);
        }

        public static string Get(string key, string defaultValue = "")
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var val))
                {
                    return val;
                }
            }

            try
            {
                using var db = new AppDbContext();
                var setting = db.AppSettings.AsNoTracking().FirstOrDefault(s => s.SettingKey == key);
                if (setting != null)
                {
                    lock (_lock)
                    {
                        _cache[key] = setting.SettingValue;
                    }
                    return setting.SettingValue;
                }
            }
            catch
            {
                // Fallback to defaultValue
            }

            return defaultValue;
        }

        public static int GetInt(string key, int defaultValue = 0)
        {
            string val = Get(key, defaultValue.ToString());
            return int.TryParse(val, out int result) ? result : defaultValue;
        }

        public static bool GetBool(string key, bool defaultValue = false)
        {
            string val = Get(key, defaultValue.ToString().ToLower());
            return bool.TryParse(val, out bool result) ? result : defaultValue;
        }

        public static void Set(string key, string value, string category = "General", string? description = null)
        {
            try
            {
                using var db = new AppDbContext();
                var setting = db.AppSettings.FirstOrDefault(s => s.SettingKey == key);
                if (setting == null)
                {
                    setting = new AppSetting
                    {
                        SettingKey = key,
                        SettingValue = value,
                        Category = category,
                        Description = description,
                        UpdatedAt = DateTime.Now
                    };
                    db.AppSettings.Add(setting);
                }
                else
                {
                    setting.SettingValue = value;
                    if (!string.IsNullOrWhiteSpace(category)) setting.Category = category;
                    if (!string.IsNullOrWhiteSpace(description)) setting.Description = description;
                    setting.UpdatedAt = DateTime.Now;
                }
                db.SaveChanges();

                lock (_lock)
                {
                    _cache[key] = value;
                }

                AuditService.Log("Update Setting", "Setting", key, $"Updated setting '{key}' to '{value}'.");
            }
            catch (Exception ex)
            {
                AuditService.Log("Setting Error", "Setting", key, $"Failed to update setting: {ex.Message}");
            }
        }

        public static string CurrencySymbol => Get("General.Currency", "Rs.");

        public static string DateFormat => Get("General.DateFormat", "dd/MM/yyyy");

        public static string FormatCurrency(decimal amount)
        {
            return $"{CurrencySymbol} {amount:N2}";
        }

        public static string FormatDate(DateTime date)
        {
            return date.ToString(DateFormat, CultureInfo.InvariantCulture);
        }

        public static List<AppSetting> GetAllSettings()
        {
            using var db = new AppDbContext();
            return db.AppSettings.AsNoTracking().OrderBy(s => s.Category).ThenBy(s => s.SettingKey).ToList();
        }
    }
}
