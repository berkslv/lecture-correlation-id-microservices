
# Implementation in .NET project

Üzerinde konuşmuş olduğumuz middlewareleri microservice mimarisi ile geliştirilmiş, HTTP ve RabbitMQ üzerinden MassTransit ile haberleşen bir uygulama üzerinde uygulayamak için ilk olarak YARP kullanacak API Gateway için Gateway projemizi, aralarında çeşitli şekillerde iletişime geçecek ve çok basit bir e ticaret sistemini modelleyen Order, Catalog ve Inventory API projelerini oluşturacağız. Contracts projesi içerisinde ise tüm API'ler arasında paylaşılan event modellerini tanımlayacağız.

```bash

dotnet new sln -n Shop
cd src/
dotnet new webapi -o Gateway
dotnet new webapi -o Order.API
dotnet new webapi -o Catalog.API
dotnet new webapi -o Inventory.API
dotnet new classlib -o Contracts
dotnet sln add src/Gateway src/Order.API  src/Catalog.API src/Inventory.API src/Notification.API src/Contracts

cd src/Order.API && dotnet add reference ../Contracts
cd ../Catalog.API && dotnet add reference ../Contracts
cd ../Inventory.API && dotnet add reference ../Contracts

```

# Gateway

Gateway projemizin içerisine girip `Yarp.ReverseProxy` paketini ekledikten sonra `ReverseProxy` servisini ekleyerek YARP konfigürasyonlarını yüklüyoruz. Ayrıca `AddTransforms` metodu ile gelen isteklere yeni bir Guid üreterek `CorrelationId` header alanına ekleyen bir middleware ekliyoruz. Bu şekilde dış internetten uygulamamıza `CorrelationId` olmadan yapılan isteklere bu değeri API Gateway otomatik olarak ekleyip uygulamalarımız arasında gezdireceğiz.

```bash

cd Gateway/
dotnet add package Yarp.ReverseProxy

```

```csharp

using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(builderContext =>
    {
        builderContext.AddRequestTransform(async transformContext =>
        {
            transformContext.ProxyRequest.Headers.Add("CorrelationId", Guid.NewGuid().ToString());
            await Task.CompletedTask;
        });
    });

var app = builder.Build();

app.MapReverseProxy();

app.Run();

```

```json

{
  "ReverseProxy": {
    "Routes": {
      "order": {
        "ClusterId": "order_api",
        "Match": {
          "Path": "/order"
        }
      },
      "debug": {
        "ClusterId": "debug_api",
        "Match": {
          "Path": "/debug"
        },
        "Transforms": [
          {
            "PathRemovePrefix": "/debug"
          },
          {
            "PathPattern": "/get"
          }
        ]
      }
    },
    "Clusters": {
      "order_api": {
        "Destinations": {
          "destination1": {
            "Address": "http://localhost:5051"
          }
        }
      },
      "catalog_api": {
        "Destinations": {
          "destination1": {
            "Address": "http://localhost:5052"
          }
        }
      },
      "inventory_api": {
        "Destinations": {
          "destination1": {
            "Address": "http://localhost:5053"
          }
        }
      },
      "notification_api": {
        "Destinations": {
          "destination1": {
            "Address": "http://localhost:5054"
          }
        }
      },
      "debug_api": {
        "Destinations": {
          "destination1": {
            "Address": "https://httpbin.org"
          }
        }
      }
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

Gatewayimizin beklediğimiz gibi çalıştığını doğrulamak içinde httpbin.org adresine bir istek atarak `CorrelationId` header alanının eklenip eklenmediğini kontrol edebiliriz.

```bash

curl http://localhost:5050/debug
{
  "args": {}, 
  "headers": {
    "Accept": "*/*", 
    "Correlationid": "3c319297-088d-4a54-94ea-a636e350c4b2", 
    "Host": "httpbin.org", 
    "Traceparent": "00-94891011bbf1976e5cf9d00d8a935c3b-3210120f5cf517cd-00", 
    "User-Agent": "curl/8.6.0", 
    "X-Amzn-Trace-Id": "Root=1-6692c03b-4043caa75c306f0028d01be3", 
    "X-Forwarded-Host": "localhost:5050"
  }, 
  "origin": "::1, 78.183.104.30", 
  "url": "https://localhost:5050/get"
}


```

# Contracts

Contracts projesi içerisinde MassTransit üzerinden RabbitMQ'ya vereceğimiz eventleri tanımlıyoruz. Bu basit örneğimizde tek bir event ile iletişimi sağlayacağız.

```csharp

namespace Contracts;

public class OrderCreatedEvent
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

```


# Order.API

Order API projemiz içerisinde MassTransit ve Serilog paketlerini ekleyiyoruz. Bu servisimiz producer rolünü üstlenerek `GET /order` isteği aldığı zaman `OrderCreatedEvent` eventini RabbitMQ'ya publish edecek. Yani HTTP istek alıp, RabbitMQ'ya event publish eden bir servis olağı için gelen HTTP isteklerinde `CorrelationMiddleware` ile gelen `CorrelationId` HTTP headerını alacak ve publish edilecek eventlerde araya girecek olan `CorrelationPublishFilter` middleware'i CorrelationId değerini MassTransit tarafından sağlanan CorrelationId header değerine set edecek.

Eğer MassTransit'e eventlerimizi `IPublishEndpoint` ile publish değil `IRequestClient<T>` send etseydik, `CorrelationSendFilter` middleware'i araya girip `CorrelationId` header alanını ekleyecekti.

```bash

cd ../Order.API/
dotnet add package MassTransit
dotnet add package MassTransit.RabbitMQ
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Elasticsearch

```


```csharp

var builder = WebApplication.CreateBuilder(args);

builder.AddMassTransit(builder.Configuration);

builder.AddSerilog(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<CorrelationMiddleware>();

app.MapGet("/order", (IPublishEndpoint publishEndpoint) =>
{
    publishEndpoint.Publish(new OrderCreatedEvent
    {
        ProductId = Guid.NewGuid().ToString(),
        Quantity = 2
    });
    
    return Results.Ok("Order processing...");
});

app.Run();

```

MassTransit tarafından kullanılacak olan Filter sınıflarımızı aşağıdaki gibi servis eklerken tanımlayabiliriz. Eklemiş olduğumuz Filter sınıflarından önce çalışan built-in Diagnostics middleware'leri tarafından loglama işlemi sırasında CorrelationId alanının loglanmaması sorununu gidermek için Serilog tanımını yaparken verdiğimiz `.MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Warning)` satırı ile Microsoft.AspNetCore.Hosting.Diagnostics loglarını Warning seviyesine çekiyoruz.

```csharp

namespace Order.API;

public static class ConfigureServices
{
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

```

```json

{
  "ProjectName": "shop-order",
  "MessageBroker": {
    "UserName": "guest",
    "Password": "guest",
    "Host": "localhost",
    "VirtualHost": "/"
  },
  "ElasticSearch": {
    "Uri": "http://localhost:9200"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"
    },
    "Using": [ "Serilog.Sinks.Console" ],
    "Enrich": [
      "WithMachineName",
      "WithEnvironmentName"
    ],
    "WriteTo": [
      {
        "Name": "Console"
      }
    ]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}

```

# Catalog.API

Catalog.API projemizde Order.API tarafından publish edilecek olan `OrderCreatedEvent` eventini consume ederek gelen isteği Inventory servisine HTTP isteği ile iletecek. Bu işlem sırasında ilk olarak gelen event için `CorrelationConsumeFilter` araya girerek Order servisimizde `CorrelationPublishFilter` veya `CorrelationSendFilter` tarafından MassTransit headerlarına eklenen `CorrelationId` header alanını alacak ve HTTP isteği yapılırken araya girecek olan `CorrelationHeaderHandler` middleware'i bu değeri HTTP headerına ekleyecek.

```bash

cd ../Order.API/
dotnet add package MassTransit
dotnet add package MassTransit.RabbitMQ
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Elasticsearch

```

```csharp

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTransient<CorrelationHeaderHandler>();

builder.Services.AddHttpClient("Inventory", c =>
    {
        c.BaseAddress = new Uri("http://localhost:5053");
    })
    .AddHttpMessageHandler<CorrelationHeaderHandler>();;

builder.AddMassTransit(builder.Configuration);

builder.AddSerilog(builder.Configuration);

builder.Services.AddScoped<IInventoryService, InventoryService>();

var app = builder.Build();

app.UseMiddleware<CorrelationMiddleware>();

app.Run();

```


```csharp

namespace Catalog.API.Consumers;

public class OrderCreatedEventConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly ILogger<OrderCreatedEventConsumer> _logger;
    private readonly IInventoryService _inventoryService;

    public OrderCreatedEventConsumer(ILogger<OrderCreatedEventConsumer> logger, IInventoryService inventoryService)
    {
        _logger = logger;
        _inventoryService = inventoryService;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        _logger.LogInformation("OrderCreatedEventConsumer: {Message}", context.Message.ProductId);
        
        var message = context.Message;
        
        await _inventoryService.RemoveStockAsync(message.ProductId, message.Quantity);

        await Task.CompletedTask;
    }
}

```

```csharp

public class InventoryService : IInventoryService
{
    private readonly IHttpClientFactory _httpClientFactory;
    
    public InventoryService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }
    
    public async Task RemoveStockAsync(string productId, int quantity)
    {
        var httpClient = _httpClientFactory.CreateClient("Inventory");
        
        var response = await httpClient.PostAsync($"remove-stock/{productId}/{quantity}", null);
        
        response.EnsureSuccessStatusCode();
    }
}

```

# Inventory.API

Inventory.API servisimizde Catalog.API tarafından yapılan HTTP isteğinde `CorrelationMiddleware` ile araya girerek `CorrelationId` değerini alıp Serilog'un LogContext'ine ekleyecek. Bu sayede loglama işlemlerinde `CorrelationId` değerini loglayabileceğiz.

```bash

cd ../Inventory.API/
dotnet add package MassTransit
dotnet add package MassTransit.RabbitMQ
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Elasticsearch

```

```csharp

var builder = WebApplication.CreateBuilder(args);

builder.AddMassTransit(builder.Configuration);

builder.AddSerilog(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<CorrelationMiddleware>();

app.MapPost("/remove-stock/{productId}/{quantity}", (ILogger logger, [FromRoute] string productId, [FromRoute] int quantity) =>
{
    logger.LogInformation("Removing stock for product {ProductId} with quantity {Quantity}", productId, quantity);
    
    return Results.Ok("Stock removed successfully");
});

app.Run();

```


# Docker Compose

Servislerimizin MassTransit üzerinden RabbitMQ ile iletişime geçmesi için rabbitmq servisini ve logları göndereceğimiz Elasticsearch servisini, son olarakta gönderdiğimiz logları görselleştirecek Kibanayı docker-compose ile ayağa kaldırıyoruz.

```yaml

version: '3'
services:
  rabbitmq:
    image: rabbitmq:3-management-alpine
    container_name: 'rabbitmq'
    ports:
      - 5672:5672
      - 15672:15672
    volumes:
      - ./.docker/rabbitmq/data/:/var/lib/rabbitmq/
      - ~./.docker/rabbitmq/log/:/var/log/rabbitmq
    networks:
      - rabbitmq
        
  elasticsearch:
    image: elasticsearch:7.16.2
    container_name: elasticsearch
    ports:
      - "9200:9200"
      - "9300:9300"
    environment:
      ES_JAVA_OPTS: "-Xmx256m -Xms256m"
      discovery.type: single-node
    networks:
      - elk

  kibana:
    image: kibana:7.16.2
    container_name: kibana
    ports:
      - "5601:5601"
    environment:
      - ELASTICSEARCH_URL=http://elasticsearch:9200
    networks:
      - elk
    depends_on:
      - elasticsearch

networks:
  elk:
    driver: bridge
  rabbitmq:
    driver: bridge
```

```bash

docker-compose up -d

```