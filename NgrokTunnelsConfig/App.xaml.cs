using System.Configuration;
using System.Data;
using System.IO;
using System;
using System.Windows;

namespace NgrokTunnelsConfig;

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
            var helpText = AppHelpText.GetHelpText();
            try
            {
                Console.WriteLine(helpText);
            }
            catch
            {
            }

            MessageBox.Show(helpText, "NgrokTunnelsConfig Help", MessageBoxButton.OK, MessageBoxImage.Information);
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

        var vm = new MainViewModel
        {
            Path = options.Path,
            AuthToken = options.AuthToken,
            Network = options.Network,
            IpBase = options.IpBase,
            PortText = effectivePort.ToString(),
        };

        if (!NetworkValidator.TryValidateLocalNetwork(vm.Network, out var networkError))
        {
            vm.Error = networkError ?? "Invalid network.";
        }

        if (!string.IsNullOrWhiteSpace(vm.AuthToken) && !AuthTokenValidator.IsValid(vm.AuthToken))
        {
            vm.Error = "Invalid authtoken. Must be exactly 49 characters and contain only letters, digits, or underscore.";
        }

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

        var window = new MainWindow
        {
            DataContext = vm,
        };

        MainWindow = window;
        window.Show();
    }
}

