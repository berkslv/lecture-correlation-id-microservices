using System.Reflection;
using MassTransit;
using Order.API.Filters.Correlation;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

namespace Order.API;

public static class ConfigureServices
{
    public static WebApplicationBuilder AddMassTransit(this WebApplicationBuilder builder, IConfiguration configuration)
    {
        var messageBroker = builder.Configuration.GetSection("MessageBroker");
        builder.Services.AddMassTransit(cfg =>
        {
            cfg.SetKebabCaseEndpointNameFormatter();
            
            cfg.AddConsumers(Assembly.GetExecutingAssembly());

            cfg.UsingRabbitMq((context, config) =>
            {
                config.UseSendFilter(typeof(CorrelationSendFilter<>), context);
                config.UsePublishFilter(typeof(CorrelationPublishFilter<>), context);
                config.UseConsumeFilter(typeof(CorrelationConsumeFilter<>), context);

                config.Host(messageBroker["Host"], messageBroker["VirtualHost"], h =>
                {
                    h.Username(messageBroker["Username"]!);
                    h.Password(messageBroker["Password"]!);
                });

                config.ConfigureEndpoints(context);
            });
        });

        return builder;
    }

    public static WebApplicationBuilder AddSerilog(this WebApplicationBuilder builder, IConfiguration configuration)
    {
        var elasticSearch = builder.Configuration.GetSection("ElasticSearch");
        var index = $"{builder.Configuration.GetValue<string>("ProjectName")}-{DateTime.Today:yyyyMMdd}";
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", builder.Configuration.GetValue<string>("ProjectName"))
            .WriteTo.Elasticsearch(
                new ElasticsearchSinkOptions(new Uri(elasticSearch["Uri"]!))
                {
                    IndexFormat = index,
                    AutoRegisterTemplate = true,
                    DetectElasticsearchVersion = true,
                })
            .ReadFrom.Configuration(builder.Configuration)
            .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Warning)
            .CreateLogger();

        builder.Logging.ClearProviders();

        builder.Host.UseSerilog(Log.Logger, true);
        
        return builder;
    }
}