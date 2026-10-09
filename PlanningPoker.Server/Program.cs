using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PlanningPoker.Server;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder();

var signalRBuilder = builder.Services.AddSignalR().AddMessagePackProtocol();
var healthChecksBuilder = builder.Services.AddHealthChecks();

if (builder.Configuration.GetConnectionString("Redis") is string redisConnectionString && !string.IsNullOrEmpty(redisConnectionString)) {
    var redis = await ConnectionMultiplexer.ConnectAsync(redisConnectionString);

    builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
    builder.Services.AddTransient(s => s.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
    builder.Services.AddTransient<IStore, RedisStore>();

    signalRBuilder.AddStackExchangeRedis(
        redisConnectionString,
        o => o.Configuration.ChannelPrefix = RedisChannel.Literal("signalr_prod")
    );

    healthChecksBuilder.AddAsyncCheck(
        "redis",
        async cancellationToken => {
            await redis.GetDatabase().PingAsync().WaitAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        },
        timeout: TimeSpan.FromSeconds(3)
    );
}
else {
    builder.Services.AddTransient<IStore, InMemoryStore>();
}

builder.Services.AddSingleton<IUserIdProvider, SessionHub.UserIdProvider>();

var app = builder.Build();

if (app.Environment.IsDevelopment()) {
    app.UseWebAssemblyDebugging();
}

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();

app.MapHub<SessionHub>("/sessions/hub");
app.MapHealthChecks("/health/live", new() { Predicate = _ => false });
app.MapHealthChecks("/health/ready");
app.MapFallbackToFile("index.html");

app.Run();

// Exposes the top-level-statements Program to PlanningPoker.IntegrationTests via WebApplicationFactory<Program>.
public partial class Program { }
