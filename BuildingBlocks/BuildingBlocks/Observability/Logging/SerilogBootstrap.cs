using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.Elasticsearch;
using Serilog.Debugging;
using System;

namespace BuildingBlocks.Logging
{
    public static class SerilogConfig
    {
        public static void Configure(WebApplicationBuilder builder, string appName)
        {
            var cfg = builder.Configuration;

            // Đọc từ "Elastic:*"
            var elasticUri = cfg["Elastic:Uri"] ?? "http://localhost:9200";
            var indexPrefix = cfg["Elastic:IndexPrefix"] ?? "ecommerce-dev";
            var enableEs = cfg.GetValue("Elastic:Enabled", true);
            var esUser = cfg["Elastic:Username"];   // có thể null
            var esPwd = cfg["Elastic:Password"];   // có thể null

            SelfLog.Enable(msg => Console.Error.WriteLine($"[SerilogSelfLog] {msg}"));

            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithExceptionDetails()
                .Enrich.WithProperty("Application", appName)
                .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
                .WriteTo.Console(outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties}{NewLine}{Exception}");

            if (enableEs)
            {
                var options = new ElasticsearchSinkOptions(new Uri(elasticUri))
                {
                    // bắt buộc cho ES 8 khi dùng client/sink 7.x
                    AutoRegisterTemplate = true,
                    AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
                    IndexFormat = $"{indexPrefix}-logs-{{0:yyyy.MM.dd}}",
                    MinimumLogEventLevel = LogEventLevel.Information,
                    BatchAction = ElasticOpType.Index,
                    EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog |
                                       EmitEventFailureHandling.RaiseCallback,
                    FailureCallback = logEvent =>
                        Console.Error.WriteLine(
                            $"Unable to submit event to Elasticsearch: {logEvent.RenderMessage()} - Exception: {logEvent.Exception?.Message}"
                        ),
                  
                    QueueSizeLimit = 10000
                };
                options.ModifyConnectionSettings = s =>
                {

                    s = s.EnableApiVersioningHeader();
                    s = s.DisablePing();
                    if (!string.IsNullOrWhiteSpace(esUser) && !string.IsNullOrWhiteSpace(esPwd))
                        s = s.BasicAuthentication(esUser, esPwd);

                    return s;
                };

                loggerConfig = loggerConfig.WriteTo.Elasticsearch(options);
            }

            Log.Logger = loggerConfig.CreateLogger();
            builder.Host.UseSerilog();
        }
    }
}
