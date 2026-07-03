using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using PlanningPoker.Client;

namespace PlanningPoker.IntegrationTests.Infrastructure;

public class TestSessionState(Uri serverBaseAddress, Func<HttpMessageHandler> handlerFactory) : SessionState(new TestEncryptionService()) {

    protected override Uri GetServerUri() =>
        new(serverBaseAddress, $"sessions/hub?participantId={ParticipantId}");

    // TestServer doesn't carry real WebSocket upgrades through its fake HttpMessageHandler,
    // so the client has to be forced onto long polling to talk to a WebApplicationFactory-hosted hub.
    protected override void ConfigureConnection(HttpConnectionOptions options) {
        options.HttpMessageHandlerFactory = _ => handlerFactory();
        options.Transports = HttpTransportType.LongPolling;
    }

    public string? TestSessionId =>
        SessionId;

    public async Task WaitForAsync(Func<bool> condition, TimeSpan? timeout = null) {
        var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnChanged(object? sender, EventArgs e) {
            if (condition()) {
                completionSource.TrySetResult();
            }
        }

        OnStateChanged += OnChanged;

        try {
            if (condition()) {
                return;
            }

            using var cancellationTokenSource = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(10));
            using var registration = cancellationTokenSource.Token.Register(() =>
                completionSource.TrySetException(new TimeoutException($"Condition was not met within {timeout ?? TimeSpan.FromSeconds(10)}."))
            );

            await completionSource.Task;
        }
        finally {
            OnStateChanged -= OnChanged;
        }
    }
}
