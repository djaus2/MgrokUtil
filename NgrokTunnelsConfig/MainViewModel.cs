using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NgrokTunnelsConfig;

internal sealed class MainViewModel : INotifyPropertyChanged
{
    private string _path = "";
    private string _rawYaml = "";
    private string? _authToken;
    private string _network = "";
    private string? _ipBase;
    private string? _portText;
    private string? _error;

    public string Path
    {
        get => _path;
        set
        {
            if (_path == value) return;
            _path = value;
            OnPropertyChanged();
        }
    }

    public string RawYaml
    {
        get => _rawYaml;
        set
        {
            if (_rawYaml == value) return;
            _rawYaml = value;
            OnPropertyChanged();
        }
    }

    public string? AuthToken
    {
        get => _authToken;
        set
        {
            if (_authToken == value) return;
            _authToken = value;
            OnPropertyChanged();
        }
    }

    public string Network
    {
        get => _network;
        set
        {
            if (_network == value) return;
            _network = value;
            OnPropertyChanged();
        }
    }

    public string? IpBase
    {
        get => _ipBase;
        set
        {
            if (_ipBase == value) return;
            _ipBase = value;
            OnPropertyChanged();
        }
    }

    public string? PortText
    {
        get => _portText;
        set
        {
            if (_portText == value) return;
            _portText = value;
            OnPropertyChanged();
        }
    }

    public string? Error
    {
        get => _error;
        set
        {
            if (_error == value) return;
            _error = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<YamlNodeViewModel> RootNodes { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
