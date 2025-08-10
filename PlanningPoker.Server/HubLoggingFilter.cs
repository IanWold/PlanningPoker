using Microsoft.AspNetCore.SignalR;

namespace PlanningPoker.Server;

public class HubLoggingFilter(ILogger<HubLoggingFilter> logger) : IHubFilter {
    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next) {
        logger.LogInformation("Client {ConnectionId} called {MethodName} with args: {Args}",
            invocationContext.Context.ConnectionId,
            invocationContext.HubMethodName,
            invocationContext.HubMethodArguments
        );

        return await next(invocationContext);
    }
}
