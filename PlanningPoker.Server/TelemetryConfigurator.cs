using AspNetCore.SignalR.OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;

namespace PlanningPoker.Server;

public static class TelemetryConfigurator {
    public static void ConfigureTelemetry(this WebApplicationBuilder builder) {
        builder.Logging.AddOpenTelemetry(logging => {
            logging.IncludeScopes = true;
            logging.IncludeFormattedMessage = true;
            logging.ParseStateValues = true;
            logging.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName: "PlanningPoker.Server", serviceVersion: "1.0.0")
            .AddAttributes(
            [
                new KeyValuePair<string, object>("deployment.environment", builder.Environment.ApplicationName)
            ]));
            logging.AddOtlpExporter();
        });
    }

    public static void ConfigureTelemetry(this IServiceCollection services, string applicationName) {
        services
        .AddOpenTelemetry()
        .ConfigureResource(rb => rb
            .AddService(serviceName: "PlanningPoker.Server", serviceVersion: "1.0.0")
            .AddAttributes(
            [
                new KeyValuePair<string, object>("deployment.environment", applicationName)
            ])
        )
        .WithTracing(b => b
            .AddSignalRInstrumentation()
            .AddAspNetCoreInstrumentation(c => {
                c.RecordException = true;
            })
            .AddHttpClientInstrumentation()
            .AddOtlpExporter()
        )
        .WithMetrics(b => b
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddMeter(
                "Microsoft.AspNetCore.Hosting",
                "Microsoft.AspNetCore.Server.Kestrel",
                "Microsoft.AspNetCore.Routing",
                "Microsoft.AspNetCore.Http.Connections",
                "Microsoft.AspNetCore.SignalR"
            )
            .AddOtlpExporter()
        );
    }
}
