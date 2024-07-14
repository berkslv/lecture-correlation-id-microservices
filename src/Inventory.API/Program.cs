using Inventory.API;
using Inventory.API.Filters.Correlation;
using Microsoft.AspNetCore.Mvc;

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