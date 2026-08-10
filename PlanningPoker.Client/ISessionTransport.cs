using Microsoft.AspNetCore.Http.Connections.Client;

namespace PlanningPoker.Client;

public interface ISessionTransport {
    Uri GetServerUri(string participantId);

    void ConfigureConnection(HttpConnectionOptions options);

    Task HandleClosedAsync();

    Task HandleCreatedAsync(string sessionId, string encryptionKey);

    Task HandleInitializedAsync(Client session);
}
