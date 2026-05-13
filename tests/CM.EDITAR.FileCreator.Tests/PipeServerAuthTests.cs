using System.IO.Pipes;
using System.Text.Json;
using CM.EDITAR.Core;
using CM.EDITAR.FileCreator;
using FluentAssertions;
using Xunit;

namespace CM.EDITAR.FileCreator.Tests;

/// <summary>
/// Direct named-pipe IPC token validation tests for <see cref="PipeServer"/>.
/// Drives the pipe end-to-end with a stub <see cref="IFileCreator"/> and a stub
/// <see cref="ISecretStore"/> so the auth path is exercised without DPAPI/Windows.
/// </summary>
public sealed class PipeServerAuthTests
{
    private sealed class StubCreator : IFileCreator
    {
        public int CallCount;
        public Task<FileCreatorResponse> CreateAsync(FileCreatorRequest request, CancellationToken ct = default)
        {
            Interlocked.Increment(ref CallCount);
            return Task.FromResult(new FileCreatorResponse { Success = true, CreatedPath = request.TargetPath });
        }
    }

    private sealed class StubSecrets : ISecretStore
    {
        private readonly string _expected;
        public StubSecrets(string expected) => _expected = expected;
        public Task<string> GetOrCreateTokenAsync(CancellationToken ct = default) => Task.FromResult(_expected);
        public Task<bool> ValidateAsync(string presented, CancellationToken ct = default)
            => Task.FromResult(string.Equals(presented, _expected, StringComparison.Ordinal));
    }

    private static string UniquePipeName() => $"cmeditar-test-{Guid.NewGuid():N}";

    private static async Task<FileCreatorResponse?> RoundTripAsync(string pipeName, FileCreatorRequest req, CancellationToken ct)
    {
        await using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(5000, ct);
        await using var writer = new StreamWriter(client, leaveOpen: true) { AutoFlush = true };
        await writer.WriteLineAsync(JsonSerializer.Serialize(req));
        using var reader = new StreamReader(client, leaveOpen: true);
        var line = await reader.ReadLineAsync(ct);
        return line is null ? null : JsonSerializer.Deserialize<FileCreatorResponse>(line);
    }

    [Fact]
    public async Task Rejects_request_with_invalid_token()
    {
        var pipe = UniquePipeName();
        var creator = new StubCreator();
        var server = new PipeServer(creator, new StubSecrets("correct-token"), pipe);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var serverTask = Task.Run(() => server.RunAsync(cts.Token));

        var resp = await RoundTripAsync(pipe, new FileCreatorRequest
        {
            TemplateId = Guid.NewGuid(),
            TargetPath = "/tmp/x",
            Token = "wrong-token",
        }, cts.Token);

        resp.Should().NotBeNull();
        resp!.Success.Should().BeFalse();
        resp.ErrorCode.Should().Be("UNAUTHORIZED");
        creator.CallCount.Should().Be(0, "FileCreator must not be invoked when token validation fails");

        cts.Cancel();
        try { await serverTask; } catch (OperationCanceledException) { }
    }

    [Fact]
    public async Task Accepts_request_with_valid_token()
    {
        var pipe = UniquePipeName();
        var creator = new StubCreator();
        var server = new PipeServer(creator, new StubSecrets("good"), pipe);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var serverTask = Task.Run(() => server.RunAsync(cts.Token));

        var resp = await RoundTripAsync(pipe, new FileCreatorRequest
        {
            TemplateId = Guid.NewGuid(),
            TargetPath = "/tmp/ok",
            Token = "good",
        }, cts.Token);

        resp.Should().NotBeNull();
        resp!.Success.Should().BeTrue();
        creator.CallCount.Should().Be(1);

        cts.Cancel();
        try { await serverTask; } catch (OperationCanceledException) { }
    }

    [Fact]
    public async Task Survives_multiple_sequential_connections()
    {
        // Regression: server previously crashed after first request because
        // maxNumberOfServerInstances was 1.
        var pipe = UniquePipeName();
        var creator = new StubCreator();
        var server = new PipeServer(creator, new StubSecrets("k"), pipe);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var serverTask = Task.Run(() => server.RunAsync(cts.Token));

        for (int i = 0; i < 3; i++)
        {
            var resp = await RoundTripAsync(pipe, new FileCreatorRequest
            {
                TemplateId = Guid.NewGuid(), TargetPath = $"/tmp/{i}", Token = "k",
            }, cts.Token);
            resp.Should().NotBeNull();
            resp!.Success.Should().BeTrue();
        }
        creator.CallCount.Should().Be(3);

        cts.Cancel();
        try { await serverTask; } catch (OperationCanceledException) { }
    }

    [Fact]
    public async Task Rejects_malformed_json()
    {
        var pipe = UniquePipeName();
        var server = new PipeServer(new StubCreator(), new StubSecrets("k"), pipe);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var serverTask = Task.Run(() => server.RunAsync(cts.Token));

        await using var client = new NamedPipeClientStream(".", pipe, PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(5000, cts.Token);
        await using var writer = new StreamWriter(client, leaveOpen: true) { AutoFlush = true };
        await writer.WriteLineAsync("{ this is not valid json");
        using var reader = new StreamReader(client, leaveOpen: true);
        var line = await reader.ReadLineAsync(cts.Token);
        var resp = JsonSerializer.Deserialize<FileCreatorResponse>(line!);
        resp!.Success.Should().BeFalse();
        resp.ErrorCode.Should().Be("BAD_REQUEST");

        cts.Cancel();
        try { await serverTask; } catch (OperationCanceledException) { }
    }
}
