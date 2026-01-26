namespace NgrokTunnelsConfig;

internal static class AppHelpText
{
    public static string GetHelpText()
    {
        return "NgrokTunnelsConfig command line options:\r\n" +
               "\r\n" +
               "  --help, -h\r\n" +
               "      Show this help and exit.\r\n" +
               "\r\n" +
               "  --clear, -c\r\n" +
               "      Clear persisted settings (Path/AuthToken) and continue.\r\n" +
               "\r\n" +
               "  --path=<file>, -p=<file>\r\n" +
               "      Path to ngrok.yml ('.yml' is appended if missing).\r\n" +
               "      If a single positional argument is provided, it is treated as the path.\r\n" +
               "\r\n" +
               "  --port=<port>, -t=<port>\r\n" +
               "      Default: 4242.\r\n" +
               "      If existing tunnels contain an addr port, that port is used unless overridden.\r\n" +
               "\r\n" +
               "  --authtoken=<token>, -a=<token>\r\n" +
               "      Optional ngrok authtoken (must be 49 chars, [A-Za-z0-9_]).\r\n" +
               "      If the config file is missing, providing a valid authtoken will create it.\r\n" +
               "\r\n" +
               "  --network=<ipv4>, --nw=<ipv4>, -n=<ipv4>\r\n" +
               "      Network IPv4 used for tunnel addr generation; must exist locally.\r\n" +
               "      Default: 192.168.0.0\r\n" +
               "\r\n" +
               "  --ipBase=<csv>, -i=<csv>\r\n" +
               "      CSV of IP last-octets (1..234).\r\n" +
               "      If value starts with '+', merges with existing tcp<ip> tunnels; otherwise replaces them.\r\n";
    }
}
