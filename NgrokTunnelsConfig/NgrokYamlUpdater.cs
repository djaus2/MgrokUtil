using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

namespace NgrokTunnelsConfig;

internal static class NgrokYamlUpdater
{
    public static string UpdateTcpTunnelsFromIpBaseYaml(string yamlText, string network, IReadOnlyList<int> ipBaseIps, bool append, int port)
    {
        if (!IPAddress.TryParse(network, out var networkIp) || networkIp.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        {
            throw new InvalidOperationException($"Invalid network IPv4: {network}");
        }

        var bytes = networkIp.GetAddressBytes();
        var prefix = $"{bytes[0]}.{bytes[1]}.{bytes[2]}";

        var yaml = new YamlStream();
        using (var reader = new StringReader(yamlText ?? string.Empty))
        {
            yaml.Load(reader);
        }

        if (yaml.Documents.Count == 0)
        {
            yaml.Documents.Add(new YamlDocument(new YamlMappingNode()));
        }

        var root = yaml.Documents[0].RootNode as YamlMappingNode;
        if (root is null)
        {
            root = new YamlMappingNode();
            yaml.Documents[0] = new YamlDocument(root);
        }

        root.Style = YamlDotNet.Core.Events.MappingStyle.Block;

        var tunnelsNode = GetOrCreateMapping(root, "tunnels");
        tunnelsNode.Style = YamlDotNet.Core.Events.MappingStyle.Block;

        var existingTcpIps = ExtractExistingTcpPorts(tunnelsNode);
        var merged = append ? existingTcpIps.Concat(ipBaseIps) : ipBaseIps;

        var finalPorts = merged
            .Where(p => p > 0 && p < 235)
            .Distinct()
            .Order()
            .ToList();

        RemoveExistingTcpTunnels(tunnelsNode);

        foreach (var ip in finalPorts)
        {
            var tunnelKey = $"tcp{ip}";
            var tunnelMap = new YamlMappingNode
            {
                { "proto", "tcp" },
                { "addr", $"{prefix}.{ip}:{port}" },
            };

            tunnelMap.Style = YamlDotNet.Core.Events.MappingStyle.Block;

            tunnelsNode.Children[new YamlScalarNode(tunnelKey)] = tunnelMap;
        }

        using var writer = new StringWriter();
        yaml.Save(writer, assignAnchors: false);
        return RemoveTrailingDocumentEndMarker(writer.ToString());
    }

    public static string RemoveTcpTunnelsFromIpBaseYaml(string yamlText, IReadOnlyList<int> ipBaseIps)
    {
        var yaml = new YamlStream();
        using (var reader = new StringReader(yamlText ?? string.Empty))
        {
            yaml.Load(reader);
        }

        if (yaml.Documents.Count == 0)
        {
            yaml.Documents.Add(new YamlDocument(new YamlMappingNode()));
        }

        var root = yaml.Documents[0].RootNode as YamlMappingNode;
        if (root is null)
        {
            root = new YamlMappingNode();
            yaml.Documents[0] = new YamlDocument(root);
        }

        root.Style = YamlDotNet.Core.Events.MappingStyle.Block;

        var tunnelsNode = GetOrCreateMapping(root, "tunnels");
        tunnelsNode.Style = YamlDotNet.Core.Events.MappingStyle.Block;

        var portsToRemove = ipBaseIps
            .Where(p => p > 0 && p < 235)
            .Distinct()
            .ToList();

        foreach (var ip in portsToRemove)
        {
            tunnelsNode.Children.Remove(new YamlScalarNode($"tcp{ip}"));
        }

        using var writer = new StringWriter();
        yaml.Save(writer, assignAnchors: false);
        return RemoveTrailingDocumentEndMarker(writer.ToString());
    }

    public static string ClearTcpTunnelsYaml(string yamlText)
    {
        var yaml = new YamlStream();
        using (var reader = new StringReader(yamlText ?? string.Empty))
        {
            yaml.Load(reader);
        }

        if (yaml.Documents.Count == 0)
        {
            yaml.Documents.Add(new YamlDocument(new YamlMappingNode()));
        }

        var root = yaml.Documents[0].RootNode as YamlMappingNode;
        if (root is null)
        {
            root = new YamlMappingNode();
            yaml.Documents[0] = new YamlDocument(root);
        }

        root.Style = YamlDotNet.Core.Events.MappingStyle.Block;

        var tunnelsNode = GetOrCreateMapping(root, "tunnels");
        tunnelsNode.Style = YamlDotNet.Core.Events.MappingStyle.Block;
        RemoveExistingTcpTunnels(tunnelsNode);

        using var writer = new StringWriter();
        yaml.Save(writer, assignAnchors: false);
        return RemoveTrailingDocumentEndMarker(writer.ToString());
    }

    public static int? TryReadPortFromExistingTcpTunnelsYaml(string yamlText)
    {
        var yaml = new YamlStream();
        try
        {
            using var reader = new StringReader(yamlText ?? string.Empty);
            yaml.Load(reader);
        }
        catch
        {
            return null;
        }

        if (yaml.Documents.Count == 0)
        {
            return null;
        }

        if (yaml.Documents[0].RootNode is not YamlMappingNode root)
        {
            return null;
        }

        if (!root.Children.TryGetValue(new YamlScalarNode("tunnels"), out var tunnelsNode) || tunnelsNode is not YamlMappingNode tunnels)
        {
            return null;
        }

        foreach (var kvp in tunnels.Children)
        {
            if (kvp.Key is not YamlScalarNode keyScalar)
            {
                continue;
            }

            var key = keyScalar.Value ?? string.Empty;
            if (!key.StartsWith("tcp", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (kvp.Value is not YamlMappingNode tunnelMap)
            {
                continue;
            }

            if (!tunnelMap.Children.TryGetValue(new YamlScalarNode("addr"), out var addrNode))
            {
                continue;
            }

            var addr = (addrNode as YamlScalarNode)?.Value;
            if (string.IsNullOrWhiteSpace(addr))
            {
                continue;
            }

            var idx = addr.LastIndexOf(':');
            if (idx < 0 || idx == addr.Length - 1)
            {
                continue;
            }

            var portText = addr[(idx + 1)..];
            if (int.TryParse(portText, out var parsedPort))
            {
                return parsedPort;
            }
        }

        return null;
    }

    public static void UpdateTcpTunnelsFromIpBase(string path, string network, IReadOnlyList<int> ipBaseIps, bool append, int port)
    {
        if (!IPAddress.TryParse(network, out var networkIp) || networkIp.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        {
            throw new InvalidOperationException($"Invalid network IPv4: {network}");
        }

        var bytes = networkIp.GetAddressBytes();
        var prefix = $"{bytes[0]}.{bytes[1]}.{bytes[2]}";

        var yaml = new YamlStream();
        using (var reader = new StreamReader(path))
        {
            yaml.Load(reader);
        }

        if (yaml.Documents.Count == 0)
        {
            yaml.Documents.Add(new YamlDocument(new YamlMappingNode()));
        }

        var root = yaml.Documents[0].RootNode as YamlMappingNode;
        if (root is null)
        {
            root = new YamlMappingNode();
            yaml.Documents[0] = new YamlDocument(root);
        }

        var tunnelsNode = GetOrCreateMapping(root, "tunnels");

        var existingTcpIps = ExtractExistingTcpPorts(tunnelsNode);
        var merged = append ? existingTcpIps.Concat(ipBaseIps) : ipBaseIps;

        var finalPorts = merged
            .Where(p => p > 0 && p < 235)
            .Distinct()
            .Order()
            .ToList();

        RemoveExistingTcpTunnels(tunnelsNode);

        foreach (var ip in finalPorts)
        {
            var tunnelKey = $"tcp{ip}";
            var tunnelMap = new YamlMappingNode
            {
                { "proto", "tcp" },
                { "addr", $"{prefix}.{ip}:{port}" },
            };

            tunnelsNode.Children[new YamlScalarNode(tunnelKey)] = tunnelMap;
        }

        using (var writer = new StreamWriter(path))
        {
            yaml.Save(writer, assignAnchors: false);
        }

        var savedText = File.ReadAllText(path);
        var trimmed = RemoveTrailingDocumentEndMarker(savedText);
        if (!string.Equals(trimmed, savedText, StringComparison.Ordinal))
        {
            File.WriteAllText(path, trimmed);
        }
    }

    public static int? TryReadPortFromExistingTcpTunnels(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        var yaml = new YamlStream();
        try
        {
            using var reader = new StreamReader(path);
            yaml.Load(reader);
        }
        catch
        {
            return null;
        }

        if (yaml.Documents.Count == 0)
        {
            return null;
        }

        if (yaml.Documents[0].RootNode is not YamlMappingNode root)
        {
            return null;
        }

        if (!root.Children.TryGetValue(new YamlScalarNode("tunnels"), out var tunnelsNode) || tunnelsNode is not YamlMappingNode tunnels)
        {
            return null;
        }

        foreach (var kvp in tunnels.Children)
        {
            if (kvp.Key is not YamlScalarNode keyScalar)
            {
                continue;
            }

            var key = keyScalar.Value ?? string.Empty;
            if (!key.StartsWith("tcp", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (kvp.Value is not YamlMappingNode tunnelMap)
            {
                continue;
            }

            if (!tunnelMap.Children.TryGetValue(new YamlScalarNode("addr"), out var addrNode))
            {
                continue;
            }

            var addr = (addrNode as YamlScalarNode)?.Value;
            if (string.IsNullOrWhiteSpace(addr))
            {
                continue;
            }

            var idx = addr.LastIndexOf(':');
            if (idx < 0 || idx == addr.Length - 1)
            {
                continue;
            }

            var portText = addr[(idx + 1)..];
            if (int.TryParse(portText, out var parsedPort))
            {
                return parsedPort;
            }
        }

        return null;
    }

    private static List<int> ExtractExistingTcpPorts(YamlMappingNode tunnelsNode)
    {
        var ports = new List<int>();
        foreach (var kvp in tunnelsNode.Children)
        {
            if (kvp.Key is not YamlScalarNode keyScalar)
            {
                continue;
            }

            var key = keyScalar.Value ?? string.Empty;
            if (!key.StartsWith("tcp", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var suffix = key[3..];
            if (suffix.Length == 0)
            {
                continue;
            }

            if (!int.TryParse(suffix, out var port))
            {
                continue;
            }

            if (port > 0 && port < 235)
            {
                ports.Add(port);
            }
        }

        return ports;
    }

    private static void RemoveExistingTcpTunnels(YamlMappingNode tunnelsNode)
    {
        var keysToRemove = new List<YamlNode>();
        foreach (var kvp in tunnelsNode.Children)
        {
            if (kvp.Key is not YamlScalarNode keyScalar)
            {
                continue;
            }

            var key = keyScalar.Value ?? string.Empty;
            if (key.StartsWith("tcp", StringComparison.OrdinalIgnoreCase))
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            tunnelsNode.Children.Remove(key);
        }
    }

    private static YamlMappingNode GetOrCreateMapping(YamlMappingNode parent, string key)
    {
        var keyNode = new YamlScalarNode(key);
        if (parent.Children.TryGetValue(keyNode, out var existing))
        {
            if (existing is YamlMappingNode existingMap)
            {
                return existingMap;
            }

            var map = new YamlMappingNode();
            parent.Children[keyNode] = map;
            return map;
        }
        else
        {
            var map = new YamlMappingNode();
            parent.Add(keyNode, map);
            return map;
        }
    }

    private static string RemoveTrailingDocumentEndMarker(string text)
    {
        var trimmed = (text ?? string.Empty).TrimEnd();
        if (trimmed.EndsWith("\n...", StringComparison.Ordinal) || trimmed.EndsWith("\r\n...", StringComparison.Ordinal))
        {
            var idx = trimmed.LastIndexOf("...", StringComparison.Ordinal);
            if (idx >= 0)
            {
                trimmed = trimmed[..idx].TrimEnd();
            }
        }

        var normalized = NormalizeTunnelsEmptyMap(trimmed);
        return normalized + Environment.NewLine;
    }

    private static string NormalizeTunnelsEmptyMap(string yamlText)
    {
        var text = yamlText ?? string.Empty;

        text = text.Replace("tunnels: {}", "tunnels:", StringComparison.Ordinal);
        text = text.Replace("tunnels: {}\r\n", "tunnels:\r\n", StringComparison.Ordinal);
        text = text.Replace("tunnels: {}\n", "tunnels:\n", StringComparison.Ordinal);

        return text;
    }
}
