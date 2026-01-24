using System.Configuration;
using System.Data;
using System.IO;
using System;
using System.Windows;

namespace MgrokUtil;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        var preliminary = CommandLineOptions.Parse(e.Args, baseSettings: null);
        if (preliminary.ShowHelp)
        {
            var helpText = GetHelpText();
            try
            {
                Console.WriteLine(helpText);
            }
            catch
            {
            }

            MessageBox.Show(helpText, "MgrokUtil Help", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown(0);
            return;
        }
        if (preliminary.ClearSettings)
        {
            try
            {
                AppSettingsStore.Delete();
            }
            catch
            {
            }
        }

        var settings = preliminary.ClearSettings ? new AppSettings(null, null, null) : AppSettingsStore.Load();
        var options = CommandLineOptions.Parse(e.Args, settings);

        var effectivePort = settings.Port ?? 4242;
        var yamlPort = NgrokYamlUpdater.TryReadPortFromExistingTcpTunnels(options.Path);
        if (yamlPort.HasValue)
        {
            effectivePort = yamlPort.Value;
        }

        if (options.Port.HasValue)
        {
            effectivePort = options.Port.Value;
        }

        if (!NetworkValidator.TryValidateLocalNetwork(options.Network, out var networkError))
        {
            MessageBox.Show(networkError ?? "Invalid network.", "MgrokUtil", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        if (!string.IsNullOrWhiteSpace(options.AuthToken) && !AuthTokenValidator.IsValid(options.AuthToken))
        {
            MessageBox.Show("Invalid authtoken. Must be exactly 49 characters and contain only letters, digits, or underscore.", "MgrokUtil", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        if (!string.IsNullOrWhiteSpace(options.IpBase))
        {
            var ipBaseText = options.IpBase.Trim();
            if (ipBaseText.StartsWith("+", StringComparison.Ordinal))
            {
                ipBaseText = ipBaseText[1..].Trim();
            }

            if (!IpBaseValidator.TryParse(ipBaseText, out _, out var ipBaseError))
            {
                MessageBox.Show(ipBaseError ?? "Invalid ipBase.", "MgrokUtil", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
                return;
            }
        }

        var defaultPath = NgrokDefaults.DefaultConfigPath();
        if (!File.Exists(options.Path))
        {
            if (!string.IsNullOrWhiteSpace(options.AuthToken))
            {
                try
                {
                    var dir = Path.GetDirectoryName(options.Path);
                    if (!string.IsNullOrWhiteSpace(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    var content = "version: \"2\"\r\n\r\nauthtoken:  " + options.AuthToken + "\r\n\r\ntunnels:\r\n";
                    File.WriteAllText(options.Path, content);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "MgrokUtil", MessageBoxButton.OK, MessageBoxImage.Error);
                    Shutdown(1);
                    return;
                }
            }
            else
            {
                if (string.Equals(options.Path, defaultPath, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show($"Default config not found: {defaultPath}\r\nProvide --authtoken to create it.", "MgrokUtil", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show($"Config not found: {options.Path}\r\nProvide --authtoken to create it.", "MgrokUtil", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                Shutdown(1);
                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(options.IpBase))
        {
            var ipBaseText = options.IpBase.Trim();
            var append = false;
            if (ipBaseText.StartsWith("+", StringComparison.Ordinal))
            {
                append = true;
                ipBaseText = ipBaseText[1..].Trim();
            }

            if (!IpBaseValidator.TryParse(ipBaseText, out var ports, out var ipBaseUpdateError))
            {
                MessageBox.Show(ipBaseUpdateError ?? "Invalid ipBase.", "MgrokUtil", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
                return;
            }

            try
            {
                NgrokYamlUpdater.UpdateTcpTunnelsFromIpBase(options.Path, options.Network, ports, append, effectivePort);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "MgrokUtil", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
                return;
            }
        }

        var vm = new MainViewModel
        {
            Path = options.Path,
            AuthToken = options.AuthToken,
            Network = options.Network,
            IpBase = options.IpBase,
        };

        try
        {
            if (File.Exists(options.Path))
            {
                var loaded = YamlLoader.Load(options.Path);
                vm.RawYaml = loaded.RawText;
                vm.RootNodes.Clear();
                foreach (var root in loaded.Roots)
                {
                    vm.RootNodes.Add(root);
                }

                vm.AuthToken = options.AuthToken ?? loaded.AuthToken;
            }
            else
            {
                vm.Error = $"File not found: {options.Path}";
            }
        }
        catch (Exception ex)
        {
            vm.Error = ex.Message;
        }

        try
        {
            AppSettingsStore.Save(new AppSettings(vm.Path, vm.AuthToken, effectivePort));
        }
        catch
        {
        }

        var window = new MainWindow
        {
            DataContext = vm,
        };

        MainWindow = window;
        window.Show();
    }

    private static string GetHelpText()
    {
        return "MgrokUtil command line options:\r\n" +
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

