using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NgrokTunnelsConfig;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private Process? _ngrokProcess;

    private List<string> Tunnels = new List<string>();

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

    private void MenuSettingsSave_Click(object sender, RoutedEventArgs e) => SaveConfig_Click(sender, e);

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

    // Start tunnelling: stop existing ngrok.exe instances then launch ngrok start --all without capturing output.
    private void MenuTunnelsStart_Click(object sender, RoutedEventArgs e)
    {
        Vm.Error = null;
        MessageBox.Show("When shell is showing:\n - Copy contents of shell but leave running.\n - Then Menu->Capture Tunnels\n - Then Menu->Select Tunnel", "grok start --all");
        try
        {
            // stop any other ngrok executables (keeps behavior you added)
            MenuTunnelsStopNgrokExe_Click(sender, e);

            // Start a new shell running ngrok start --all and keep shell alive.
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/k ngrok start --all",
                UseShellExecute = true, // no redirection, run in its own console
                CreateNoWindow = false,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
            };
                        var proc = Process.Start(psi);
            if (proc == null)
            {
                Vm.Error = "Failed to start ngrok process.";
                return;
            }

            // track the started process so Stop can target it
            _ngrokProcess = proc;
            Vm.Error = $"Started ngrok (PID {proc.Id}).";
        }
        catch (Exception ex)
        {
            Vm.Error = ex.Message;
        }
    }

    private async void MenuTunnelsStopNgrok_Click(object sender, RoutedEventArgs e)
    {
        Vm.Error = null;

        try
        {
            if (_ngrokProcess == null)
            {
                Vm.Error = "No ngrok process started by this app.";
                return;
            }

            var proc = _ngrokProcess;

            // If process already exited, clean up and inform user.
            if (proc.HasExited)
            {
                try { proc.Dispose(); } catch { }
                _ngrokProcess = null;
                Vm.Error = "ngrok process has already exited.";
                return;
            }

            // Stop on background thread to avoid blocking UI.
            var stopped = await Task.Run(() =>
            {
                try
                {
                    // Try a polite close first.
                    try
                    {
                        if (proc.CloseMainWindow())
                        {
                            if (proc.WaitForExit(3000))
                                return true;
                        }
                    }
                    catch
                    {
                        // ignore and fallback to Kill
                    }

                    // Fallback: Kill process (and tree if supported)
                    try
                    {
                        proc.Kill(entireProcessTree: true);
                    }
                    catch
                    {
                        try { proc.Kill(); } catch { }
                    }

                    // wait for exit
                    return proc.WaitForExit(3000);
                }
                catch
                {
                    return false;
                }
            }).ConfigureAwait(true);

            if (stopped)
            {
                try { proc.Dispose(); } catch { }
                _ngrokProcess = null;
                Vm.Error = $"Stopped ngrok (PID {proc.Id}).";
            }
            else
            {
                Vm.Error = "Failed to stop ngrok cleanly; process may still be running.";
            }
        }
        catch (Exception ex)
        {
            Vm.Error = ex.Message;
        }
        finally
        {
            // Just in case, attempt to stop any remaining ngrok processes to avoid orphans.
            MenuTunnelsStopNgrokExe_Click(sender, e);
        }
    }

    private void MenuTunnelsStopNgrokExe_Click(object sender, RoutedEventArgs e)
    {
        var uiDispatcher = Dispatcher;
        var vmLocal = (MainViewModel)DataContext; // safe: running on UI thread

        // Attempt to stop any existing ngrok processes first.
        try
        {
            var existing = Process.GetProcessesByName("ngrok");
            if (existing.Length > 0)
            {
                var stopped = 0;
                foreach (var p in existing)
                {
                    try
                    {
                        if (!p.HasExited)
                        {
                            // Try a quick graceful close, then kill if it doesn't respond.
                            try
                            {
                                p.CloseMainWindow();
                                if (!p.WaitForExit(1000))
                                {
                                    p.Kill();
                                    p.WaitForExit(2000);
                                }
                            }
                            catch
                            {
                                try
                                {
                                    p.Kill();
                                    p.WaitForExit(2000);
                                }
                                catch { /* ignore individual kill failures */ }
                            }
                        }

                        stopped++;
                    }
                    catch (Exception ex)
                    {
                        // surface per-process errors to the UI but continue with others
                        vmLocal.Error = $"Failed to stop ngrok PID {p.Id}: {ex.Message}";
                    }
                    finally
                    {
                        try { p.Dispose(); } catch { }
                    }
                }

                if (stopped > 0)
                {
                    vmLocal.Error = $"Stopped {stopped} existing ngrok process(es).";
                }
            }
        }
        catch (Exception ex)
        {
            vmLocal.Error = $"Error enumerating ngrok processes: {ex.Message}";
        }
    }

    private void MenuTunnelsCapture_Click(object sender, RoutedEventArgs e)
    {
        Vm.Error = null;

        try
        {
            if (!Clipboard.ContainsText())
            {
                Vm.Error = "Clipboard does not contain text.";
                return;
            }

            var clipboardText = Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(clipboardText))
            {
                Vm.Error = "Clipboard text is empty.";
                return;
            }

            // Split into lines (preserve empty lines but they will be ignored)
            var lines = clipboardText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            var results = new List<string>();

            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                var line = raw.TrimStart();

                // Only lines starting with "Forwarding"
                if (!line.StartsWith("Forwarding", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Find "tcp" token and capture until next space
                var idx = line.IndexOf("tcp", StringComparison.OrdinalIgnoreCase);
                if (idx < 0)
                    continue;

                var end = line.IndexOf(' ', idx);
                var token = end > idx ? line.Substring(idx, end - idx) : line.Substring(idx);
                token = token.Trim();

                if (!string.IsNullOrEmpty(token) && !results.Contains(token, StringComparer.OrdinalIgnoreCase))
                {
                    results.Add(token);
                }
            }

            if (results.Count == 0)
            {
                Vm.Error = "No 'Forwarding' tcp URLs found in clipboard.";
                return;
            }
            Tunnels = results;
            var csv = string.Join(",", results);
            Clipboard.SetText(csv);
            Vm.Error = $"Captured {results.Count} forwarding(s) to clipboard.";
        }
        catch (Exception ex)
        {
            Vm.Error = ex.Message;
        }
    }

    private void MenuTunnelSelect_Click(object sender, RoutedEventArgs e)
    {
        Vm.Error = null;

        try
        {
            if (Tunnels == null || Tunnels.Count == 0)
            {
                Vm.Error = "No tunnels available. Run Capture first.";
                return;
            }

            var dlg = new Window
            {
                Title = "Select Tunnel",
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var listBox = new System.Windows.Controls.ListBox
            {
                MinWidth = 600,
                MinHeight = 200
            };

            listBox.MouseDoubleClick += (s, a) =>
            {
                if (listBox.SelectedItem is string sel)
                {
                    Clipboard.SetText(sel);
                    Vm.Error = "Selected tunnel copied to clipboard.";
                    dlg.DialogResult = true;
                    dlg.Close();
                }
            };
            listBox.KeyDown += (s, a) =>
            {
                if (a is KeyEventArgs ke && ke.Key == Key.Enter)
                {
                    if (listBox.SelectedItem is string sel)
                    {
                        Clipboard.SetText(sel);
                        Vm.Error = "Selected tunnel copied to clipboard.";
                        dlg.DialogResult = true;
                        dlg.Close();
                    }
                }
            };

            foreach (var t in Tunnels)
                listBox.Items.Add(t);

            var ok = new System.Windows.Controls.Button { Content = "OK", IsDefault = true, Width = 75, Margin = new Thickness(5) };
            var cancel = new System.Windows.Controls.Button { Content = "Cancel", IsCancel = true, Width = 75, Margin = new Thickness(5) };
            ok.Click += (s, a) =>
            {
                if (listBox.SelectedItem is string sel)
                {
                    Clipboard.SetText(sel);
                    Vm.Error = "Selected tunnel copied to clipboard.";
                    dlg.DialogResult = true;
                }
                else
                {
                    Vm.Error = "No item selected.";
                    dlg.DialogResult = false;
                }

                dlg.Close();
            };
            cancel.Click += (s, a) => { dlg.DialogResult = false; dlg.Close(); };

            var btnPanel = new System.Windows.Controls.StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            btnPanel.Children.Add(ok);
            btnPanel.Children.Add(cancel);

            var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(10) };
            panel.Children.Add(listBox);
            panel.Children.Add(btnPanel);

            dlg.Content = panel;

            var result = dlg.ShowDialog();
            if (result == true && listBox.SelectedItem is string selected)
            {
                Clipboard.SetText(selected);
                Vm.Error = "Selected tunnel copied to clipboard.";
            }
            else
            {
                Vm.Error = "Selection cancelled or no item selected.";
            }
        }
        catch (Exception ex)
        {
            Vm.Error = ex.Message;
        }
    }
}