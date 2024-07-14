using Contracts;
using MassTransit;
using Order.API;
using Order.API.Filters.Correlation;

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
