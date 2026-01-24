using System;
using System.IO;

namespace MgrokUtil;

internal static class NgrokDefaults
{
    public static string DefaultConfigPath()
    {
        var homePath = Environment.GetEnvironmentVariable("HOMEPATH");
        if (string.IsNullOrWhiteSpace(homePath))
        {
            homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        return Path.Combine(homePath, "AppData", "Local", "ngrok", "ngrok.yml");
    }
}
