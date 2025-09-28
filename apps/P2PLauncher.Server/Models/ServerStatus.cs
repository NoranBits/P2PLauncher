using System.Text.Json;
using System.Text.Json.Serialization;

namespace P2PLauncher.Server.Models;

public class ServerStatus
{
    public bool Running { get; set; }
    public DateTime Timestamp { get; set; }
    public int PeerCount { get; set; }
    public PeerInfo[] CurrentPeers { get; set; } = [];
    public PeerEvent[] RecentPeerEvents { get; set; } = [];
    public int[] OpenTcpPorts { get; set; } = [];
    public int[] OpenUdpPorts { get; set; } = [];
    public string[] RecentSavedHosts { get; set; } = [];
    public string[] RecentLogLines { get; set; } = [];
}

public class PeerInfo
{
    public string Ip { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime Since { get; set; }
}

public class PeerEvent
{
    public DateTime Time { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? PeerIp { get; set; }
    public string? PeerId { get; set; }
    public string? PeerName { get; set; }
    public string? Reason { get; set; }
}

public class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return DateTime.Parse(reader.GetString()!).ToUniversalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
    }
}