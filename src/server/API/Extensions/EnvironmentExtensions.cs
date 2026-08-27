using System;
using System.IO;

namespace Server.API.Extensions;

public static class EnvironmentExtensions
{
    public static void LoadEnvironmentVariables()
    {
        var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        if (!File.Exists(envPath))
        {
            envPath = Path.Combine(AppContext.BaseDirectory, ".env");
        }

        if (!File.Exists(envPath)) return;

        foreach (var line in File.ReadAllLines(envPath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;

            var parts = trimmed.Split('=', 2);
            if (parts.Length == 2)
            {
                var key = parts[0].Trim();
                var value = parts[1].Trim();
                if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length >= 2)
                {
                    value = value.Substring(1, value.Length - 2);
                }
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
