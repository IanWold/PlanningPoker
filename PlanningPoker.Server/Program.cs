using AspNetCore.SignalR.OpenTelemetry;
using Microsoft.AspNetCore.SignalR;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PlanningPoker.Server;
using StackExchange.Redis;
using OpenTelemetry.Metrics;
using Serilog;

var builder = WebApplication.CreateBuilder(
    new WebApplicationOptions {
        Args = args,
        ContentRootPath = "./"
    }
);

builder.Host.UseSerilog((_, c) => c
    .WriteTo.Console()
    .WriteTo.OpenTelemetry(
        endpoint: Environment.GetEnvironmentVariable("OTEL_COLLECTOR_ENDPOINT")!,
        resourceAttributes: new Dictionary<string, object> {
            { "service.name", builder.Environment.ApplicationName }
        }
    )
);

if (Environment.GetEnvironmentVariable("PORT") is not null and { Length: > 0 } portVar && int.TryParse(portVar, out int port)) {
    builder.WebHost.ConfigureKestrel(
        options => {
            options.ListenAnyIP(port);
        }
    );
}

var signalRBuilder = builder.Services.AddSignalR()
    .AddMessagePackProtocol()
    .AddHubInstrumentation();

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

builder.Services.AddSingleton<IUserIdProvider, SessionHub.UserIdProvider>();

builder.Services.AddRazorPages();

builder.Services.AddOpenTelemetry()
    .WithTracing(b => b
        .AddSignalRInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .ConfigureResource(c => {
            c.AddService(builder.Environment.ApplicationName);
        })
        .AddOtlpExporter(c => {
            c.Endpoint = new Uri(Environment.GetEnvironmentVariable("OTEL_COLLECTOR_ENDPOINT")!);
        })
    )
    .WithMetrics(b => b
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddPrometheusExporter()
    );

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
app.MapPrometheusScrapingEndpoint("/metrics");
app.MapHub<SessionHub>("/sessions/hub");
app.MapFallbackToFile("index.html");

app.Run();
