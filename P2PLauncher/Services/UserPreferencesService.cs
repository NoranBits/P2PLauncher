using Newtonsoft.Json;
using System;
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
            string appDir = AppDomain.CurrentDomain.BaseDirectory ?? ".";
            prefsPath = Path.Combine(appDir, "client.defaults.json");
        }

        public ClientDefaults LoadClient()
        {
            try
            {
                if (File.Exists(prefsPath))
                {
                    string json = File.ReadAllText(prefsPath);
                    var obj = JsonConvert.DeserializeObject<ClientDefaults>(json);
                    return obj ?? new ClientDefaults();
                }
            }
            catch
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
