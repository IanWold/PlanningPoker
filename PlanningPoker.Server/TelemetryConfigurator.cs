using AspNetCore.SignalR.OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;
using OpenTelemetry.Exporter;

namespace PlanningPoker.Server;

public static class TelemetryConfigurator {
    public record Options(string? Url, string ApplicationName, string Environment, string Version) {
        public bool HasTelemetry => Url is not null;

        public ResourceBuilder GetResourceBuilder(ResourceBuilder resourceBuilder) =>
            resourceBuilder
            .AddService(serviceName: ApplicationName, serviceVersion: Version)
            .AddAttributes(
            [
                new KeyValuePair<string, object>("deployment.environment", Environment)
            ]);

        public void ConfigureExporter(OtlpExporterOptions options) {
            options.Endpoint = new Uri(Url!);
            options.Protocol = OtlpExportProtocol.HttpProtobuf;
        }
    }

    public static void ConfigureTelemetry(this WebApplicationBuilder builder, Options options) {
        if (!options.HasTelemetry) {
            return;
        }

        builder.Logging.AddOpenTelemetry(logging => {
            logging.IncludeScopes = true;
            logging.IncludeFormattedMessage = true;
            logging.ParseStateValues = true;
            logging.SetResourceBuilder(options.GetResourceBuilder(ResourceBuilder.CreateDefault()));
            logging.AddOtlpExporter(options.ConfigureExporter);
        });
    }

    public static void ConfigureTelemetry(this IServiceCollection services, Options options) {
        if (!options.HasTelemetry) {
            return;
        }

        services
        .AddOpenTelemetry()
        .ConfigureResource(rb => options.GetResourceBuilder(rb))
        .WithTracing(b => b
            .AddSignalRInstrumentation()
            .AddAspNetCoreInstrumentation(c => {
                c.RecordException = true;
            })
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(options.ConfigureExporter)
        );
    }
}
