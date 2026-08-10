using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.JSInterop;

namespace PlanningPoker.Client.Web;

file class JsBridge(Client session) {
    [JSInvokable("LeaveAsync")]
    public Task LeaveFromJsAsync() =>
        session.LeaveAsync();
}

public class WebSessionTransport(NavigationManager navigationManager, IJSRuntime jsRuntime) : ISessionTransport {
    public Uri GetServerUri(string participantId) =>
        navigationManager.ToAbsoluteUri($"/sessions/hub?participantId={participantId}");

    public void ConfigureConnection(HttpConnectionOptions options) { }

    public Task HandleClosedAsync() {
        navigationManager.NavigateTo("/", true);
        return Task.CompletedTask;
    }

    public Task HandleCreatedAsync(string sessionId, string encryptionKey) {
        navigationManager.NavigateTo($"/session/{sessionId}#key={encryptionKey}");
        return Task.CompletedTask;
    }

    public async Task HandleInitializedAsync(Client session) =>
        await jsRuntime.InvokeVoidAsync("setupSignalRBeforeUnloadListener", DotNetObjectReference.Create(new JsBridge(session)));
}
