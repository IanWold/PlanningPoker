using System.Text.Json;
using Microsoft.AspNetCore.SignalR;

namespace PlanningPoker.Server;

public class HubLoggingFilter(ILogger<HubLoggingFilter> logger) : IHubFilter {
    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next) {
        _ = Task.Run(() => logger.LogInformation("Client {ConnectionId} called {MethodName} with args: {Args}",
            invocationContext.Context.ConnectionId,
            invocationContext.HubMethodName,
            string.Join(", ", invocationContext.HubMethodArguments.Select(o => JsonSerializer.Serialize(o)))
        ));

        var result = await next(invocationContext);

        if (result is not null) {
            _ = Task.Run(() => logger.LogInformation("Call to {MethodName} from client {ConnectionId} responding with: {Result}",
                invocationContext.Context.ConnectionId,
                invocationContext.HubMethodName,
                JsonSerializer.Serialize(result)
            ));
        }

        return result;
    }
}
