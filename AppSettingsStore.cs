using System;
using System.IO;
using System.Text.Json;

namespace MgrokUtil;

internal sealed record AppSettings(string? Path, string? AuthToken);

internal static class AppSettingsStore
{
    private static string SettingsPath()
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return System.IO.Path.Combine(baseDir, "MgrokUtil", "settings.json");
    }

    public static void Delete()
    {
        var path = SettingsPath();
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public static AppSettings Load()
    {
        var path = SettingsPath();
        if (!File.Exists(path))
        {
            return new AppSettings(null, null);
        }

        try
        {
            var json = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            return settings ?? new AppSettings(null, null);
        }
        catch
        {
            return new AppSettings(null, null);
        }
    }

    public static void Save(AppSettings settings)
    {
        var path = SettingsPath();
        var dir = System.IO.Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }
}
