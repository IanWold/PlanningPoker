using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace PlanningPoker.Client.Web;

public class WebSessionState(NavigationManager navigationManager, IJSRuntime jsRuntime, IEncryptionService encryptionService) : SessionState(encryptionService) {
    protected override Uri GetServerUri() =>
        navigationManager.ToAbsoluteUri($"/sessions/hub?participantId={ParticipantId}");

    protected override  async Task HandleClosedAsync() =>
        navigationManager.NavigateTo("/", true);

    protected override  async Task HandleCreatedAsync() =>
        navigationManager.NavigateTo($"/session/{SessionId}#key={EncryptionKey}");

    protected override  async Task HandleInitializedAsync(SessionState instance) =>
        await jsRuntime.InvokeVoidAsync("setupSignalRBeforeUnloadListener", DotNetObjectReference.Create(this));

    [JSInvokable("LeaveAsync")]
    public async Task LeaveFromJsAsync() =>
        await LeaveAsync();
}
