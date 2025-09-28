using Newtonsoft.Json;
using System.IO;

namespace P2PLauncher.Services
{
    internal sealed class UserPreferencesService
    {
        private readonly string prefsPath;
        private readonly string connectionsPath;
        private readonly Lock fileLock = new();

        internal sealed class ClientDefaults
        {
            public string Host { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string Id { get; set; } = string.Empty;
            public bool Relay { get; set; }
            public bool ShowDebug { get; set; }
        }

        internal sealed class ConnectionEntry
        {
            public string Host { get; set; } = string.Empty;
            public int Port { get; set; }
            public DateTimeOffset When { get; set; } = DateTimeOffset.UtcNow;
        }

        public UserPreferencesService()
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory ?? ".";
            prefsPath = Path.Combine(appDir, "client.defaults.json");
            connectionsPath = Path.Combine(appDir, "connections.json");
        }

        public ClientDefaults LoadClient()
        {
            try
            {
                if (File.Exists(prefsPath))
                {
                    var json = File.ReadAllText(prefsPath);
                    ClientDefaults? obj = JsonConvert.DeserializeObject<ClientDefaults>(json);
                    return obj ?? new ClientDefaults();
                }
            }
            catch (IOException)
            {
                // ignore and fall back to defaults
            }
            catch (UnauthorizedAccessException)
            {
                // ignore and fall back to defaults
            }
            catch (JsonException)
            {
                // ignore and fall back to defaults
            }
            return new ClientDefaults();
        }

        public void SaveClient(ClientDefaults defaults)
        {
            ArgumentNullException.ThrowIfNull(defaults);
            var json = JsonConvert.SerializeObject(defaults, Formatting.Indented);
            File.WriteAllText(prefsPath, json);
        }

        /// <summary>
        /// Save a successful connection entry. Keeps most recent 100 unique entries (host+port).
        /// </summary>
        public void SaveSuccessfulConnection(string host, int port)
        {
            ArgumentNullException.ThrowIfNull(host);
            lock (fileLock)
            {
                List<ConnectionEntry> list;
                try
                {
                    if (File.Exists(connectionsPath))
                    {
                        var existing = File.ReadAllText(connectionsPath);
                        list = JsonConvert.DeserializeObject<List<ConnectionEntry>>(existing) ?? [];
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

                // Remove any identical existing entry (explicitly ignore removed count)
                _ = list.RemoveAll(e => string.Equals(e.Host, host, StringComparison.OrdinalIgnoreCase) && e.Port == port);

                // Insert at front
                list.Insert(0, new ConnectionEntry { Host = host, Port = port, When = DateTimeOffset.UtcNow });

                // Keep up to 100 entries
                if (list.Count > 100)
                {
                    list = [.. list.Take(100)];
                }

                try
                {
                    var json = JsonConvert.SerializeObject(list, Formatting.Indented);
                    File.WriteAllText(connectionsPath, json);
                }
                catch (IOException)
                {
                    // Swallow IO errors to avoid impacting runtime
                }
            }
        }

        /// <summary>
        /// Returns distinct hosts (host only) from saved successful connections, ordered by most recent.
        /// </summary>
        public IReadOnlyList<string> GetRecentHosts(int max = 50)
        {
            lock (fileLock)
            {
                try
                {
                    if (!File.Exists(connectionsPath))
                    {
                        return [];
                    }

                    var json = File.ReadAllText(connectionsPath);
                    List<ConnectionEntry> list = JsonConvert.DeserializeObject<List<ConnectionEntry>>(json) ?? [];

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
                    return [];
                }
                catch (UnauthorizedAccessException)
                {
                    return [];
                }
                catch (JsonException)
                {
                    return [];
                }
            }
        }
    }
}
