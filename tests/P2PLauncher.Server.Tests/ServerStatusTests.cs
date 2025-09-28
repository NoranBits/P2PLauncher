using System.Diagnostics;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace P2PLauncher.Server.Tests
{
    public class ServerStatusTests
    {
        [Fact]
        public void StatusCommand_ProducesJsonWithExpectedFields()
        {
            // Arrange
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "run --project ..\\apps\\P2PLauncher.Server\\P2PLauncher.Server.csproj -- status",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var p = Process.Start(psi)!;
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(5000);

            // Assert
            output.Should().NotBeNullOrWhiteSpace();
            var doc = JsonDocument.Parse(output);
            doc.RootElement.TryGetProperty("Running", out _).Should().BeTrue();
            doc.RootElement.TryGetProperty("PeerCount", out _).Should().BeTrue();
            doc.RootElement.TryGetProperty("RecentPeerEvents", out _).Should().BeTrue();
        }
    }
}
