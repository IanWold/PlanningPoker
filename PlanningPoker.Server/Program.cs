using Microsoft.AspNetCore.SignalR;
using PlanningPoker.Server;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(
    new WebApplicationOptions {
        Args = args,
        ContentRootPath = "./"
    }
);

if (Environment.GetEnvironmentVariable("PORT") is not null and { Length: > 0 } portVar && int.TryParse(portVar, out int port)) {
    builder.WebHost.ConfigureKestrel(
        options => {
            options.ListenAnyIP(port);
        }
    );
}

var signalRBuilder = builder.Services.AddSignalR();

if (builder.Configuration.GetConnectionString("Redis") is string redisConnectionString && !string.IsNullOrEmpty(redisConnectionString)) {
    builder.Services.AddSingleton<IConnectionMultiplexer>(await ConnectionMultiplexer.ConnectAsync(redisConnectionString));
    builder.Services.AddTransient(s => s.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
    builder.Services.AddTransient<IStore, RedisStore>();
    signalRBuilder.AddStackExchangeRedis(
        redisConnectionString,
        options => {
            options.Configuration.ChannelPrefix = RedisChannel.Literal("signalr_prod");
        }
    );
}
else {
    builder.Services.AddTransient<IStore, InMemoryStore>();
}

signalRBuilder.AddMessagePackProtocol();

builder.Services.AddSingleton<IUserIdProvider, SessionHub.UserIdProvider>();

builder.Services.AddRazorPages();

var app = builder.Build();

if (app.Environment.IsDevelopment()) {
    app.UseWebAssemblyDebugging();
}
else {
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();
app.MapHub<SessionHub>("/sessions/hub", options => {
    options.AllowStatefulReconnects = true;
});
app.MapFallbackToFile("index.html");

app.Run();
