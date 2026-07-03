using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace PlanningPoker.Client.Web;

public class EncryptionService(IJSRuntime jsRuntime) : IEncryptionService {
    public async Task<string> DecryptAsync(string value) =>
        await jsRuntime.InvokeAsync<string>("decrypt", value);

    public async Task<string> EncryptAsync(string value) =>
        await jsRuntime.InvokeAsync<string>("encrypt", value);

    public async Task<string> GetKeyAsync() =>
        await jsRuntime.InvokeAsync<string>("getEncryptionKey") ?? string.Empty;
}
