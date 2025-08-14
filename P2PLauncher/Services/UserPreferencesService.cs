using Newtonsoft.Json;
using System.IO;

namespace P2PLauncher.Services
{
    internal sealed class UserPreferencesService
    {
        private readonly string prefsPath;

        internal sealed class ClientDefaults
        {
            public string Host { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string Id { get; set; } = string.Empty;
            public bool Relay { get; set; }
            public bool ShowDebug { get; set; }
        }

        public UserPreferencesService()
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory ?? ".";
            prefsPath = Path.Combine(appDir, "client.defaults.json");
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
    }
}
