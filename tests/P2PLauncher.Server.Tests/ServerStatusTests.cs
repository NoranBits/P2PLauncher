using System.Diagnostics;
using System.Text.Json;
using FluentAssertions;
using P2PLauncher.Server.Models;
using Xunit;

namespace P2PLauncher.Server.Tests;

public class ServerStatusTests
{
    [Fact]
    public async Task StatusCommand_ProducesValidJsonWithExpectedFields()
    {
        // Arrange
        var serverPath = Path.Combine("../../apps/P2PLauncher.Server/bin/Debug/net9.0/P2PLauncher.Server");
        var psi = new ProcessStartInfo
        {
            FileName = serverPath,
            Arguments = "status",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Act
        using var process = Process.Start(psi)!;
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();
        
        var output = await outputTask;
        var error = await errorTask;

        // Debug output
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Process failed with exit code {process.ExitCode}. Error: {error}");
        }

        // Extract JSON from output (skip log lines)
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var jsonLine = lines.FirstOrDefault(line => line.TrimStart().StartsWith('{'));

        // Assert
        process.ExitCode.Should().Be(0);
        jsonLine.Should().NotBeNullOrWhiteSpace("status command should output JSON");

        // Verify JSON structure matches ServerStatus model
        var status = JsonSerializer.Deserialize<ServerStatus>(jsonLine!, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        status.Should().NotBeNull();
        status!.Running.Should().Be(true);
        status.PeerCount.Should().BeGreaterOrEqualTo(0);
        status.CurrentPeers.Should().NotBeNull();
        status.RecentPeerEvents.Should().NotBeNull();
        status.OpenTcpPorts.Should().NotBeNull();
        status.OpenUdpPorts.Should().NotBeNull();
        status.RecentSavedHosts.Should().NotBeNull();
        status.RecentLogLines.Should().NotBeNull();
        status.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task TestPortsCommand_ReturnsPortArrays()
    {
        // Arrange
        var serverPath = Path.Combine("../../apps/P2PLauncher.Server/bin/Debug/net9.0/P2PLauncher.Server");
        var psi = new ProcessStartInfo
        {
            FileName = serverPath,
            Arguments = "test-ports",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Act
        using var process = Process.Start(psi)!;
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        // Debug output
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Process failed with exit code {process.ExitCode}. Error: {error}");
        }

        // Extract JSON from output (skip log lines)
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var jsonLine = lines.FirstOrDefault(line => line.TrimStart().StartsWith('{'));

        // Assert
        process.ExitCode.Should().Be(0);
        jsonLine.Should().NotBeNullOrWhiteSpace();

        var result = JsonSerializer.Deserialize<JsonDocument>(jsonLine!);
        result.RootElement.TryGetProperty("tcpPorts", out var tcpPorts).Should().BeTrue();
        result.RootElement.TryGetProperty("udpPorts", out var udpPorts).Should().BeTrue();
        result.RootElement.TryGetProperty("sampleTcpPorts", out var sampleTcp).Should().BeTrue();
        result.RootElement.TryGetProperty("sampleUdpPorts", out var sampleUdp).Should().BeTrue();

        tcpPorts.ValueKind.Should().Be(JsonValueKind.Array);
        udpPorts.ValueKind.Should().Be(JsonValueKind.Array);
        sampleTcp.ValueKind.Should().Be(JsonValueKind.Array);
        sampleUdp.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task PrintConfigCommand_ReturnsValidConfiguration()
    {
        // Arrange
        var serverPath = Path.Combine("../../apps/P2PLauncher.Server/bin/Debug/net9.0/P2PLauncher.Server");
        var psi = new ProcessStartInfo
        {
            FileName = serverPath,
            Arguments = "print-config",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Act
        using var process = Process.Start(psi)!;
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        // Debug output
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Process failed with exit code {process.ExitCode}. Error: {error}");
        }

        // Extract JSON from output (skip log lines)
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var jsonLine = lines.FirstOrDefault(line => line.TrimStart().StartsWith('{'));

        // Assert
        process.ExitCode.Should().Be(0);
        jsonLine.Should().NotBeNullOrWhiteSpace();

        var result = JsonSerializer.Deserialize<JsonDocument>(jsonLine!);
        result.RootElement.TryGetProperty("serverName", out var serverName).Should().BeTrue();
        result.RootElement.TryGetProperty("version", out var version).Should().BeTrue();
        result.RootElement.TryGetProperty("targetFramework", out var framework).Should().BeTrue();

        serverName.GetString().Should().Be("P2PLauncher.Server");
        version.GetString().Should().Be("1.0.0");
        framework.GetString().Should().Be("net9.0");
    }
}
