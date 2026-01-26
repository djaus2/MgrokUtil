using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace NgrokTunnelsConfig;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private MainViewModel Vm => (MainViewModel)DataContext;

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "YAML files (*.yml;*.yaml)|*.yml;*.yaml|All files (*.*)|*.*",
            CheckFileExists = false,
        };

        if (dlg.ShowDialog(this) == true)
        {
            Vm.Path = dlg.FileName;
        }
    }

    private void Load_Click(object sender, RoutedEventArgs e)
    {
        Vm.Error = null;

        try
        {
            if (!File.Exists(Vm.Path))
            {
                Vm.RawYaml = string.Empty;
                Vm.RootNodes.Clear();
                Vm.Error = $"File not found: {Vm.Path}";
                return;
            }

            var loaded = YamlLoader.Load(Vm.Path);
            Vm.RawYaml = loaded.RawText;
            Vm.RootNodes.Clear();
            foreach (var root in loaded.Roots)
            {
                Vm.RootNodes.Add(root);
            }

            Vm.AuthToken ??= loaded.AuthToken;
        }
        catch (Exception ex)
        {
            Vm.Error = ex.Message;
        }
    }

    private void ApplyIpBase_Click(object sender, RoutedEventArgs e)
    {
        ApplyIpBaseInternal(append: false);
    }

    private void ApplyIpBaseInternal(bool append)
    {
        Vm.Error = null;

        try
        {
            if (!NetworkValidator.TryValidateLocalNetwork(Vm.Network, out var networkError))
            {
                Vm.Error = networkError ?? "Invalid network.";
                return;
            }

            if (!string.IsNullOrWhiteSpace(Vm.AuthToken) && !AuthTokenValidator.IsValid(Vm.AuthToken))
            {
                Vm.Error = "Invalid authtoken. Must be exactly 49 characters and contain only letters, digits, or underscore.";
                return;
            }

            if (string.IsNullOrWhiteSpace(Vm.IpBase))
            {
                Vm.Error = "IpBase is empty.";
                return;
            }

            var ipBaseText = Vm.IpBase.Trim();
            if (ipBaseText.StartsWith("+", StringComparison.Ordinal))
            {
                Vm.Error = "IpBase should not include '+'; use Tunnels -> Add instead.";
                return;
            }

            if (!IpBaseValidator.TryParse(ipBaseText, out var ipBaseIps, out var ipBaseError))
            {
                Vm.Error = ipBaseError ?? "Invalid ipBase.";
                return;
            }

            var effectivePort = GetEffectivePortFromUiAndPreview(Vm.Path);
            Vm.PortText = effectivePort.ToString();

            var baseYaml = GetCurrentYamlText();
            var updatedYaml = NgrokYamlUpdater.UpdateTcpTunnelsFromIpBaseYaml(baseYaml, Vm.Network, ipBaseIps, append, effectivePort);
            SetPreviewYaml(updatedYaml);
        }
        catch (Exception ex)
        {
            Vm.Error = ex.Message;
        }
    }

    private void SaveConfig_Click(object sender, RoutedEventArgs e)
    {
        Vm.Error = null;

        try
        {
            if (string.IsNullOrWhiteSpace(Vm.Path))
            {
                Vm.Error = "Path is empty.";
                return;
            }

            if (!Vm.Path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
            {
                Vm.Path += ".yml";
            }

            var dir = Path.GetDirectoryName(Vm.Path);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var yamlText = GetCurrentYamlText();
            File.WriteAllText(Vm.Path, yamlText);

            Load_Click(sender, e);
        }
        catch (Exception ex)
        {
            Vm.Error = ex.Message;
        }
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        Vm.Error = null;

        try
        {
            var port = GetEffectivePortFromUiAndPreview(Vm.Path);
            Vm.PortText = port.ToString();
            AppSettingsStore.Save(new AppSettings(Vm.Path, Vm.AuthToken, port));
        }
        catch (Exception ex)
        {
            Vm.Error = ex.Message;
        }
    }

    private void ClearSettings_Click(object sender, RoutedEventArgs e)
    {
        Vm.Error = null;

        try
        {
            AppSettingsStore.Delete();
        }
        catch (Exception ex)
        {
            Vm.Error = ex.Message;
        }
    }

    private void Help_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(AppHelpText.GetHelpText(), "NgrokTunnelsConfig Help", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void MenuFileLoad_Click(object sender, RoutedEventArgs e) => Load_Click(sender, e);

    private void MenuFileSave_Click(object sender, RoutedEventArgs e) => SaveConfig_Click(sender, e);

    private void MenuTunnelsAdd_Click(object sender, RoutedEventArgs e) => ApplyIpBaseInternal(append: true);

    private void MenuTunnelsReplace_Click(object sender, RoutedEventArgs e) => ApplyIpBaseInternal(append: false);

    private void MenuTunnelsRemove_Click(object sender, RoutedEventArgs e)
    {
        Vm.Error = null;

        try
        {
            if (string.IsNullOrWhiteSpace(Vm.IpBase))
            {
                Vm.Error = "IpBase is empty.";
                return;
            }

            var ipBaseText = Vm.IpBase.Trim();
            if (ipBaseText.StartsWith("+", StringComparison.Ordinal))
            {
                Vm.Error = "IpBase should not include '+'; use Tunnels -> Add instead.";
                return;
            }

            if (!IpBaseValidator.TryParse(ipBaseText, out var ipBaseIps, out var ipBaseError))
            {
                Vm.Error = ipBaseError ?? "Invalid ipBase.";
                return;
            }

            var baseYaml = GetCurrentYamlText();
            var updatedYaml = NgrokYamlUpdater.RemoveTcpTunnelsFromIpBaseYaml(baseYaml, ipBaseIps);
            SetPreviewYaml(updatedYaml);
        }
        catch (Exception ex)
        {
            Vm.Error = ex.Message;
        }
    }

    private void MenuTunnelsClear_Click(object sender, RoutedEventArgs e)
    {
        Vm.Error = null;

        try
        {
            var baseYaml = GetCurrentYamlText();
            var updatedYaml = NgrokYamlUpdater.ClearTcpTunnelsYaml(baseYaml);
            SetPreviewYaml(updatedYaml);
        }
        catch (Exception ex)
        {
            Vm.Error = ex.Message;
        }
    }

    private void MenuSettingsSave_Click(object sender, RoutedEventArgs e) => SaveSettings_Click(sender, e);

    private void MenuSettingsClear_Click(object sender, RoutedEventArgs e) => ClearSettings_Click(sender, e);

    private void MenuHelp_Click(object sender, RoutedEventArgs e) => Help_Click(sender, e);

    private static void EnsureConfigFileExists(string path, string? authToken)
    {
        if (File.Exists(path))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(authToken))
        {
            throw new InvalidOperationException($"Config not found: {path}\r\nProvide an authtoken to create it.");
        }

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var content = "version: \"2\"\r\n\r\nauthtoken:  " + authToken + "\r\n\r\ntunnels:\r\n";
        File.WriteAllText(path, content);
    }

    private int GetEffectivePortFromUiAndPreview(string yamlPath)
    {
        var settings = AppSettingsStore.Load();

        var effective = settings.Port ?? 4242;

        var yamlText = GetCurrentYamlText();
        var previewPort = NgrokYamlUpdater.TryReadPortFromExistingTcpTunnelsYaml(yamlText);
        if (previewPort.HasValue)
        {
            effective = previewPort.Value;
        }

        var uiText = Vm.PortText?.Trim();
        if (!string.IsNullOrWhiteSpace(uiText) && int.TryParse(uiText, out var parsed))
        {
            effective = parsed;
        }

        return effective;
    }

    private string GetCurrentYamlText()
    {
        if (!string.IsNullOrWhiteSpace(Vm.RawYaml))
        {
            return Vm.RawYaml;
        }

        if (!string.IsNullOrWhiteSpace(Vm.Path) && File.Exists(Vm.Path))
        {
            return File.ReadAllText(Vm.Path);
        }

        if (!string.IsNullOrWhiteSpace(Vm.AuthToken))
        {
            return "version: \"2\"\r\n\r\nauthtoken:  " + Vm.AuthToken + "\r\n\r\ntunnels:\r\n";
        }

        return "version: \"2\"\r\n\r\nauthtoken:  \r\n\r\ntunnels:\r\n";
    }

    private void SetPreviewYaml(string yamlText)
    {
        var loaded = YamlLoader.LoadFromText(yamlText);
        Vm.RawYaml = loaded.RawText;
        Vm.RootNodes.Clear();
        foreach (var root in loaded.Roots)
        {
            Vm.RootNodes.Add(root);
        }

        Vm.AuthToken ??= loaded.AuthToken;
    }
}