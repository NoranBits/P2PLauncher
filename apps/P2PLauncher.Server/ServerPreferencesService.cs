using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace P2PLauncher.Server
{
    internal sealed class ServerPreferencesService
    {
        private readonly string connectionsPath;
        private readonly object fileLock = new object();

        internal sealed record ConnectionEntry(string Host, int Port, DateTimeOffset When);

        public ServerPreferencesService()
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory ?? ".";
            connectionsPath = Path.Combine(appDir, "connections-server.json");
        }

        public void SaveSuccessfulConnection(string host, int port)
        {
            if (string.IsNullOrWhiteSpace(host)) throw new ArgumentNullException(nameof(host));

            lock (fileLock)
            {
                List<ConnectionEntry> list;
                try
                {
                    if (File.Exists(connectionsPath))
                    {
                        var existing = File.ReadAllText(connectionsPath);
                        list = JsonSerializer.Deserialize<List<ConnectionEntry>>(existing) ?? new List<ConnectionEntry>();
                    }
                    else
                    {
                        list = new List<ConnectionEntry>();
                    }
                }
                catch
                {
                    list = new List<ConnectionEntry>();
                }

                // Remove duplicates (host+port) and insert newest at front
                list.RemoveAll(e => string.Equals(e.Host, host, StringComparison.OrdinalIgnoreCase) && e.Port == port);
                list.Insert(0, new ConnectionEntry(host, port, DateTimeOffset.UtcNow));

                // keep recent 200
                if (list.Count > 200) list = list.Take(200).ToList();

                try
                {
                    var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(connectionsPath, json);
                }
                catch
                {
                    // swallow
                }
            }
        }

        public IReadOnlyList<string> GetRecentHosts(int max = 50)
        {
            lock (fileLock)
            {
                try
                {
                    if (!File.Exists(connectionsPath)) return Array.Empty<string>();
                    var json = File.ReadAllText(connectionsPath);
                    var list = JsonSerializer.Deserialize<List<ConnectionEntry>>(json) ?? new List<ConnectionEntry>();
                    var result = list.Select(e => e.Host).Where(h => !string.IsNullOrWhiteSpace(h)).Distinct(StringComparer.OrdinalIgnoreCase).Take(max).ToList();
                    return result;
                }
                catch
                {
                    return Array.Empty<string>();
                }
            }
        }
    }
}
