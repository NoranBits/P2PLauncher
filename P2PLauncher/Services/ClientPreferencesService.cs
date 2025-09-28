using System.IO;
using System.Text.Json;

namespace P2PLauncher.Services
{
    internal sealed class ClientPreferencesService
    {
        private const int MaxRecent = 50;
        private static readonly JsonSerializerOptions s_options = new() { WriteIndented = true };
        private readonly string filePath;

        public ClientPreferencesService()
        {
            filePath = Path.Combine(AppContext.BaseDirectory, "recent-hosts.json");
        }

        public IReadOnlyList<string> LoadRecentHosts(int max = MaxRecent)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return [];
                }

                using FileStream fs = File.OpenRead(filePath);
                List<string> list = JsonSerializer.Deserialize<List<string>>(fs) ?? [];
                if (max > 0 && list.Count > max)
                {
                    list.RemoveRange(max, list.Count - max);
                }
                return list;
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

        public void SaveSuccessfulConnection(string host, int port)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                return;
            }
            if (port is < 1 or > 65535)
            {
                return;
            }

            var entry = $"{host}:{port}";

            List<string> list;
            try
            {
                list = [.. LoadRecentHosts(MaxRecent)];
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

            _ = list.RemoveAll(x => string.Equals(x, entry, StringComparison.OrdinalIgnoreCase));
            list.Insert(0, entry);
            if (list.Count > MaxRecent)
            {
                list.RemoveRange(MaxRecent, list.Count - MaxRecent);
            }

            try
            {
                _ = Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                using FileStream fs = File.Create(filePath);
                JsonSerializer.Serialize(fs, list, s_options);
            }
            catch (IOException)
            {
                // ignore
            }
            catch (UnauthorizedAccessException)
            {
                // ignore
            }
        }
    }
}
