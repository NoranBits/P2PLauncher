using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace P2PLauncher.Server
{
    internal sealed class ServerPreferencesService
    {
        private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly string connectionsPath;
        private readonly Lock fileLock = new();

        internal sealed record ConnectionEntry(string Host, int Port, DateTimeOffset When);

        public ServerPreferencesService()
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory ?? ".";
            connectionsPath = Path.Combine(appDir, "connections-server.json");
        }

        public void SaveSuccessfulConnection(string host, int port)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                throw new ArgumentNullException(nameof(host));
            }

            List<ConnectionEntry> list;
            using (fileLock.EnterScope())
            {
                try
                {
                    if (File.Exists(connectionsPath))
                    {
                        var existing = File.ReadAllText(connectionsPath);
                        list = JsonSerializer.Deserialize<List<ConnectionEntry>>(existing, s_jsonOptions) ?? [];
                    }
                    else
                    {
                        list = [];
                    }
                }
                catch (IOException)
                {
                    list = [];
                }
                catch (UnauthorizedAccessException)
                {
                    list = [];
                }
                catch (JsonException)
                {
                    list = [];
                }

                // Remove duplicates (host+port) and insert newest at front
                _ = list.RemoveAll(e => string.Equals(e.Host, host, StringComparison.OrdinalIgnoreCase) && e.Port == port);
                list.Insert(0, new ConnectionEntry(host, port, DateTimeOffset.UtcNow));

                // keep recent 200
                if (list.Count > 200)
                {
                    list = list.Take(200).ToList();
                }

                try
                {
                    var json = JsonSerializer.Serialize(list, s_jsonOptions);
                    File.WriteAllText(connectionsPath, json);
                }
                catch (IOException)
                {
                    // swallow
                }
                catch (UnauthorizedAccessException)
                {
                    // swallow
                }
            }
        }

        public IReadOnlyList<string> GetRecentHosts(int max = 50)
        {
            using (fileLock.EnterScope())
            {
                try
                {
                    if (!File.Exists(connectionsPath))
                    {
                        return Array.Empty<string>();
                    }

                    var json = File.ReadAllText(connectionsPath);
                    var list = JsonSerializer.Deserialize<List<ConnectionEntry>>(json, s_jsonOptions) ?? [];
                    var result = list
                        .Select(e => e.Host)
                        .Where(h => !string.IsNullOrWhiteSpace(h))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Take(max)
                        .ToList();

                    return result;
                }
                catch (IOException)
                {
                    return Array.Empty<string>();
                }
                catch (UnauthorizedAccessException)
                {
                    return Array.Empty<string>();
                }
                catch (JsonException)
                {
                    return Array.Empty<string>();
                }
            }
        }
    }
}
