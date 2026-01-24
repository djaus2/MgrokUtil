using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using YamlDotNet.RepresentationModel;

namespace MgrokUtil;

internal static class NgrokYamlUpdater
{
    public static void UpdateTcpTunnelsFromIpBase(string path, string network, IReadOnlyList<int> ipBasePorts, bool append)
    {
        if (!IPAddress.TryParse(network, out var networkIp) || networkIp.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        {
            throw new InvalidOperationException($"Invalid network IPv4: {network}");
        }

        var bytes = networkIp.GetAddressBytes();
        var hostIp = new IPAddress(new byte[] { bytes[0], bytes[1], bytes[2], 11 });
        var host = hostIp.ToString();

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

        var existingTcpPorts = ExtractExistingTcpPorts(tunnelsNode);
        var merged = append ? existingTcpPorts.Concat(ipBasePorts) : ipBasePorts;

        var finalPorts = merged
            .Where(p => p > 0 && p < 235)
            .Distinct()
            .Order()
            .ToList();

        RemoveExistingTcpTunnels(tunnelsNode);

        foreach (var port in finalPorts)
        {
            var tunnelKey = $"tcp{port}";
            var tunnelMap = new YamlMappingNode
            {
                { "proto", "tcp" },
                { "addr", $"{host}:{port}" },
            };

            tunnelsNode.Children[new YamlScalarNode(tunnelKey)] = tunnelMap;
        }

        using (var writer = new StreamWriter(path))
        {
            yaml.Save(writer, assignAnchors: false);
        }

        var savedText = File.ReadAllText(path);
        var trimmed = savedText.TrimEnd();
        if (trimmed.EndsWith("\n...", StringComparison.Ordinal) || trimmed.EndsWith("\r\n...", StringComparison.Ordinal))
        {
            var idx = trimmed.LastIndexOf("...", StringComparison.Ordinal);
            if (idx >= 0)
            {
                trimmed = trimmed[..idx].TrimEnd();
                File.WriteAllText(path, trimmed + Environment.NewLine);
            }
        }
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
}
