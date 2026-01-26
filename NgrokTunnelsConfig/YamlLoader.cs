using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.RepresentationModel;

namespace NgrokTunnelsConfig;

internal static class YamlLoader
{
    public static (string RawText, List<YamlNodeViewModel> Roots, string? AuthToken) Load(string path)
    {
        var rawText = File.ReadAllText(path);

        return LoadFromText(rawText);
    }

    public static (string RawText, List<YamlNodeViewModel> Roots, string? AuthToken) LoadFromText(string rawText)
    {

        var yaml = new YamlStream();
        using var reader = new StringReader(rawText);
        yaml.Load(reader);

        var roots = new List<YamlNodeViewModel>();
        string? authToken = null;
        for (var i = 0; i < yaml.Documents.Count; i++)
        {
            var docRoot = yaml.Documents[i].RootNode;
            var vm = new YamlNodeViewModel($"Document {i + 1}");
            AddNode(vm, docRoot, nameHint: "root");
            roots.Add(vm);

            authToken ??= TryGetTopLevelAuthToken(docRoot);
        }

        return (rawText, roots, authToken);
    }

    private static string? TryGetTopLevelAuthToken(YamlNode root)
    {
        if (root is not YamlMappingNode mapping)
        {
            return null;
        }

        foreach (var kvp in mapping.Children)
        {
            var key = (kvp.Key as YamlScalarNode)?.Value;
            if (!string.Equals(key, "authtoken", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (kvp.Value is YamlScalarNode valueScalar)
            {
                return valueScalar.Value;
            }

            return kvp.Value.ToString();
        }

        return null;
    }

    private static void AddNode(YamlNodeViewModel parent, YamlNode node, string nameHint)
    {
        switch (node)
        {
            case YamlScalarNode scalar:
            {
                parent.Children.Add(new YamlNodeViewModel(nameHint, scalar.Value));
                break;
            }
            case YamlMappingNode mapping:
            {
                var mapVm = new YamlNodeViewModel(nameHint);
                foreach (var kvp in mapping.Children)
                {
                    var key = (kvp.Key as YamlScalarNode)?.Value ?? kvp.Key.ToString();
                    AddNode(mapVm, kvp.Value, key);
                }
                parent.Children.Add(mapVm);
                break;
            }
            case YamlSequenceNode sequence:
            {
                var seqVm = new YamlNodeViewModel(nameHint);
                var idx = 0;
                foreach (var child in sequence.Children)
                {
                    AddNode(seqVm, child, $"[{idx}]" );
                    idx++;
                }
                parent.Children.Add(seqVm);
                break;
            }
            default:
            {
                parent.Children.Add(new YamlNodeViewModel(nameHint, node.ToString()));
                break;
            }
        }
    }
}
