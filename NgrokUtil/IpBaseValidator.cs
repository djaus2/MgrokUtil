using System;
using System.Collections.Generic;

namespace MgrokUtil;

internal static class IpBaseValidator
{
    public static bool TryParse(string csv, out List<int> values, out string? error)
    {
        values = new List<int>();
        error = null;

        if (string.IsNullOrWhiteSpace(csv))
        {
            error = "ipBase is empty.";
            return false;
        }

        var parts = csv.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            error = "ipBase is empty.";
            return false;
        }

        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
            {
                error = "ipBase contains an empty item.";
                return false;
            }

            if (!int.TryParse(part, out var value))
            {
                error = $"ipBase contains non-integer value: {part}";
                return false;
            }

            if (value <= 0 || value >= 235)
            {
                error = $"ipBase value out of range (must be 1..234): {value}";
                return false;
            }

            values.Add(value);
        }

        return true;
    }
}
