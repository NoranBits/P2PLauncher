using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace P2PLauncher.Server
{
    internal sealed class PeerEvent
    {
        public DateTimeOffset Time { get; init; } = DateTimeOffset.UtcNow;
        public string Type { get; init; } = string.Empty; // joined | left | error | info
        public string? PeerIp { get; init; }
        public string? Reason { get; init; }
        public override string ToString()
        {
            return PeerIp is null ? $"[{Time:HH:mm:ss}] {Type}: {Reason}" : $"[{Time:HH:mm:ss}] {Type} {PeerIp}: {Reason}";
        }
    }

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
                }

                AddEvent(new PeerEvent { Type = "joined", PeerIp = ip, Reason = line });
                return;
            }
            if (DisconnectedRegex.IsMatch(line))
            {
                if (ip != null)
                {
                    _ = peers.TryRemove(ip, out _);
                }

                AddEvent(new PeerEvent { Type = "left", PeerIp = ip, Reason = line });
                return;
            }
            if (ErrorRegex.IsMatch(line))
            {
                AddEvent(new PeerEvent { Type = "error", PeerIp = ip, Reason = line });
                return;
            }
            // Info fallback only if it contains a 9.0.0.x IP
            if (ip != null)
            {
                AddEvent(new PeerEvent { Type = "info", PeerIp = ip, Reason = line });
            }
        }

        private static string? ExtractIp(string line)
        {
            Match m = Ip9Regex.Match(line);
            return m.Success ? $"9.0.0.{m.Groups["oct"].Value}" : null;
        }

        private void AddEvent(PeerEvent ev)
        {
            lock (gate)
            {
                _ = events.AddLast(ev);
                while (events.Count > maxEvents)
                {
                    events.RemoveFirst();
                }
            }
        }

        public int PeerCount => peers.Count;
        public IReadOnlyCollection<string> CurrentPeers => [.. peers.Keys];

        public IEnumerable<PeerEvent> Recent(int last)
        {
            lock (gate)
            {
                return [.. events.Reverse().Take(Math.Max(1, last))];
            }
        }
    }
}
