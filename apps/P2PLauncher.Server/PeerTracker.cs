using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Threading;

namespace P2PLauncher.Server
{
    internal sealed class PeerEvent
    {
        public DateTimeOffset Time { get; init; } = DateTimeOffset.UtcNow;
        public string Type { get; init; } = string.Empty; // joined | left | error | info
        public string? PeerIp { get; init; }
        public string? PeerId { get; init; }
        public string? PeerName { get; init; }
        public string? Reason { get; init; }
        public override string ToString()
        {
            var idPart = string.IsNullOrEmpty(PeerId) ? string.Empty : $" id={PeerId}";
            var namePart = string.IsNullOrEmpty(PeerName) ? string.Empty : $" name={PeerName}";
            return PeerIp is null
                ? $"[{Time:HH:mm:ss}] {Type}:{idPart}{namePart} {Reason}"
                : $"[{Time:HH:mm:ss}] {Type} {PeerIp}:{idPart}{namePart} {Reason}";
        }
    }

    internal static class IdentityStore
    {
        private static readonly ConcurrentDictionary<string, string> PeerIds = new();
        private static readonly ConcurrentDictionary<string, string> PeerNames = new();

        public static (string id, string name) GetOrCreate(string ip)
        {
            if (PeerIds.TryGetValue(ip, out var existingId) && PeerNames.TryGetValue(ip, out var existingName))
            {
                return (existingId, existingName);
            }

            var guid = Guid.NewGuid().ToString("N");
            var id = guid[..8];
            var name = $"Guest-{id}";

            PeerIds[ip] = id;
            PeerNames[ip] = name;

            return (id, name);
        }

        public static string? GetIdOrNull(string ip)
        {
            return PeerIds.TryGetValue(ip, out var id) ? id : null;
        }

        public static string? GetNameOrNull(string ip)
        {
            return PeerNames.TryGetValue(ip, out var name) ? name : null;
        }

        public static void Clear()
        {
            PeerIds.Clear();
            PeerNames.Clear();
        }
    }

    internal sealed record PeerInfo(string Ip, string? Id, string? Name, DateTimeOffset Since);

    internal sealed class PeerTracker(int maxEventBuffer = 1000)
    {
        private readonly ConcurrentDictionary<string, DateTimeOffset> peers = new();
        private readonly int maxEvents = Math.Max(100, Math.Min(5000, maxEventBuffer));
        private readonly LinkedList<PeerEvent> events = new();
        private readonly Lock gate = new();

        private static readonly Regex Ip9Regex = new("\\b9\\.0\\.0\\.(?<oct>\\d{1,3})\\b", RegexOptions.Compiled);
        private static readonly Regex ConnectedRegex = new("(?i)(connected|connection\\s+established)", RegexOptions.Compiled);
        private static readonly Regex DisconnectedRegex = new("(?i)(disconnected|connection\\s+closed)", RegexOptions.Compiled);
        private static readonly Regex ErrorRegex = new("(?i)(error|failed|timeout|refused|denied)", RegexOptions.Compiled);

        public void ProcessLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            var ip = ExtractIp(line);
            if (ConnectedRegex.IsMatch(line))
            {
                if (ip != null)
                {
                    peers[ip] = DateTimeOffset.UtcNow;
                    var (id, name) = IdentityStore.GetOrCreate(ip);
                    AddEvent(new PeerEvent { Type = "joined", PeerIp = ip, PeerId = id, PeerName = name, Reason = line });
                    return;
                }

                AddEvent(new PeerEvent { Type = "joined", PeerIp = null, Reason = line });
                return;
            }
            if (DisconnectedRegex.IsMatch(line))
            {
                if (ip != null)
                {
                    peers.TryRemove(ip, out _);
                    var id = IdentityStore.GetIdOrNull(ip);
                    var name = IdentityStore.GetNameOrNull(ip);
                    AddEvent(new PeerEvent { Type = "left", PeerIp = ip, PeerId = id, PeerName = name, Reason = line });
                }
                else
                {
                    AddEvent(new PeerEvent { Type = "left", PeerIp = null, Reason = line });
                }

                return;
            }
            if (ErrorRegex.IsMatch(line))
            {
                if (ip is not null)
                {
                    var id = IdentityStore.GetIdOrNull(ip);
                    var name = IdentityStore.GetNameOrNull(ip);
                    AddEvent(new PeerEvent { Type = "error", PeerIp = ip, PeerId = id, PeerName = name, Reason = line });
                }
                else
                {
                    AddEvent(new PeerEvent { Type = "error", PeerIp = null, Reason = line });
                }
                return;
            }
            // Info fallback only if it contains a 9.0.0.x IP
            if (ip != null)
            {
                var id = IdentityStore.GetIdOrNull(ip);
                var name = IdentityStore.GetNameOrNull(ip);
                AddEvent(new PeerEvent { Type = "info", PeerIp = ip, PeerId = id, PeerName = name, Reason = line });
            }
        }

        private static string? ExtractIp(string line)
        {
            Match m = Ip9Regex.Match(line);
            return m.Success ? $"9.0.0.{m.Groups["oct"].Value}" : null;
        }

        private void AddEvent(PeerEvent ev)
        {
            using (gate.EnterScope())
            {
                events.AddLast(ev);
                while (events.Count > maxEvents)
                {
                    events.RemoveFirst();
                }
            }
        }

        public static void ClearAllIds()
        {
            IdentityStore.Clear();
        }

        public int PeerCount => peers.Count;
        public IReadOnlyCollection<string> CurrentPeers => [.. peers.Keys];

        public IEnumerable<PeerEvent> Recent(int last)
        {
            using (gate.EnterScope())
            {
                return [.. events.Reverse().Take(Math.Max(1, last))];
            }
        }

        public IReadOnlyList<PeerInfo> GetCurrentPeers()
        {
            var snapshot = peers.ToArray();
            var list = new List<PeerInfo>(snapshot.Length);
            foreach (var kv in snapshot)
            {
                var ip = kv.Key;
                var since = kv.Value;
                var id = IdentityStore.GetIdOrNull(ip);
                var name = IdentityStore.GetNameOrNull(ip);
                list.Add(new PeerInfo(ip, id, name, since));
            }
            return list.OrderBy(p => p.Ip, StringComparer.Ordinal).ToArray();
        }
    }
}
