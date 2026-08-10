using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using PlanningPoker.Client;

namespace PlanningPoker.IntegrationTests;

// TestServer doesn't carry real WebSocket upgrades through its fake HttpMessageHandler,
// so the client has to be forced onto long polling to talk to a WebApplicationFactory-hosted hub.
public class TestSessionTransport(Uri serverBaseAddress, Func<HttpMessageHandler> handlerFactory) : ISessionTransport {
    public Uri GetServerUri(string participantId) =>
        new(serverBaseAddress, $"sessions/hub?participantId={participantId}");

    public void ConfigureConnection(HttpConnectionOptions options) {
        options.HttpMessageHandlerFactory = _ => handlerFactory();
        options.Transports = HttpTransportType.LongPolling;
    }

    public Task HandleClosedAsync() =>
        Task.CompletedTask;

    public Task HandleCreatedAsync(string sessionId, string encryptionKey) =>
        Task.CompletedTask;

    public Task HandleInitializedAsync(Client.Client session) =>
        Task.CompletedTask;
}
