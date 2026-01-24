namespace MgrokUtil;

internal sealed record CommandLineOptions(string Path, string? AuthToken, string Network, string? IpBase, bool ClearSettings, bool ShowHelp)
{
    public static CommandLineOptions Parse(string[] args)
    {
        return Parse(args, baseSettings: null);
    }

    public static CommandLineOptions Parse(string[] args, AppSettings? baseSettings)
    {
        string? path = null;
        string? authToken = null;
        string? network = null;
        string? ipBase = null;
        var clearSettings = false;
        var showHelp = false;

        string? pendingKey = null;

        foreach (var argRaw in args)
        {
            if (string.IsNullOrWhiteSpace(argRaw))
            {
                continue;
            }

            var arg = argRaw.Trim();

            if (pendingKey is not null)
            {
                var value = arg.Trim().Trim('"');
                switch (pendingKey)
                {
                    case "path":
                        path = value;
                        break;
                    case "authtoken":
                        authToken = value;
                        break;
                    case "network":
                        network = value;
                        break;
                    case "ipBase":
                        ipBase = value;
                        break;
                }

                pendingKey = null;
                continue;
            }

            if (arg.StartsWith("--path=", StringComparison.OrdinalIgnoreCase))
            {
                path = arg["--path=".Length..].Trim().Trim('"');
                continue;
            }

            if (string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase))
            {
                showHelp = true;
                continue;
            }

            if (string.Equals(arg, "--clear", StringComparison.OrdinalIgnoreCase))
            {
                clearSettings = true;
                continue;
            }

            if (string.Equals(arg, "--path", StringComparison.OrdinalIgnoreCase))
            {
                pendingKey = "path";
                continue;
            }

            if (arg.StartsWith("-p=", StringComparison.OrdinalIgnoreCase))
            {
                path = arg["-p=".Length..].Trim().Trim('"');
                continue;
            }

            if (string.Equals(arg, "-p", StringComparison.OrdinalIgnoreCase))
            {
                pendingKey = "path";
                continue;
            }

            if (string.Equals(arg, "-c", StringComparison.OrdinalIgnoreCase))
            {
                clearSettings = true;
                continue;
            }

            if (string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase))
            {
                showHelp = true;
                continue;
            }

            if (arg.StartsWith("--authtoken=", StringComparison.OrdinalIgnoreCase))
            {
                authToken = arg["--authtoken=".Length..].Trim().Trim('"');
                continue;
            }

            if (string.Equals(arg, "--authtoken", StringComparison.OrdinalIgnoreCase))
            {
                pendingKey = "authtoken";
                continue;
            }

            if (arg.StartsWith("-a=", StringComparison.OrdinalIgnoreCase))
            {
                authToken = arg["-a=".Length..].Trim().Trim('"');
                continue;
            }

            if (string.Equals(arg, "-a", StringComparison.OrdinalIgnoreCase))
            {
                pendingKey = "authtoken";
                continue;
            }

            if (arg.StartsWith("--network=", StringComparison.OrdinalIgnoreCase))
            {
                network = arg["--network=".Length..].Trim().Trim('"');
                continue;
            }

            if (string.Equals(arg, "--network", StringComparison.OrdinalIgnoreCase))
            {
                pendingKey = "network";
                continue;
            }

            if (arg.StartsWith("--nw=", StringComparison.OrdinalIgnoreCase))
            {
                network = arg["--nw=".Length..].Trim().Trim('"');
                continue;
            }

            if (string.Equals(arg, "--nw", StringComparison.OrdinalIgnoreCase))
            {
                pendingKey = "network";
                continue;
            }

            if (arg.StartsWith("-n=", StringComparison.OrdinalIgnoreCase))
            {
                network = arg["-n=".Length..].Trim().Trim('"');
                continue;
            }

            if (string.Equals(arg, "-n", StringComparison.OrdinalIgnoreCase))
            {
                pendingKey = "network";
                continue;
            }

            if (arg.StartsWith("--ipBase=", StringComparison.OrdinalIgnoreCase))
            {
                ipBase = arg["--ipBase=".Length..].Trim().Trim('"');
                continue;
            }

            if (string.Equals(arg, "--ipBase", StringComparison.OrdinalIgnoreCase))
            {
                pendingKey = "ipBase";
                continue;
            }

            if (arg.StartsWith("-i=", StringComparison.OrdinalIgnoreCase))
            {
                ipBase = arg["-i=".Length..].Trim().Trim('"');
                continue;
            }

            if (string.Equals(arg, "-i", StringComparison.OrdinalIgnoreCase))
            {
                pendingKey = "ipBase";
                continue;
            }

            if (!arg.StartsWith("-", StringComparison.Ordinal) && path is null)
            {
                path = arg.Trim().Trim('"');
                continue;
            }
        }

        path ??= baseSettings?.Path ?? NgrokDefaults.DefaultConfigPath();
        authToken ??= baseSettings?.AuthToken;

        if (!path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
        {
            path += ".yml";
        }

        network ??= "192.168.0.0";

        return new CommandLineOptions(path, authToken, network, ipBase, clearSettings, showHelp);
    }
}
