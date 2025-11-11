using Microsoft.AspNetCore.SignalR;
using PlanningPoker.Server;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder();

var signalRBuilder = builder.Services.AddSignalR().AddMessagePackProtocol();

if (builder.Configuration.GetConnectionString("Redis") is string redisConnectionString && !string.IsNullOrEmpty(redisConnectionString)) {
    builder.Services.AddSingleton<IConnectionMultiplexer>(await ConnectionMultiplexer.ConnectAsync(redisConnectionString));
    builder.Services.AddTransient(s => s.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
    builder.Services.AddTransient<IStore, RedisStore>();
    
    signalRBuilder.AddStackExchangeRedis(
        redisConnectionString,
        o => o.Configuration.ChannelPrefix = RedisChannel.Literal("signalr_prod")
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
app.MapFallbackToFile("index.html");

app.Run();
