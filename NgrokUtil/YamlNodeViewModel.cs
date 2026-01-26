using System.Collections.ObjectModel;

namespace MgrokUtil;

internal sealed class YamlNodeViewModel
{
    public string Name { get; }
    public string? Value { get; }

    public ObservableCollection<YamlNodeViewModel> Children { get; } = new();

    public YamlNodeViewModel(string name, string? value = null)
    {
        Name = name;
        Value = value;
    }

    public override string ToString()
    {
        return Value is null ? Name : $"{Name}: {Value}";
    }
}
