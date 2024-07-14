using Catalog.API;
using Catalog.API.Filters.Correlation;
using Catalog.API.Services;
using Catalog.API.Services.Interfaces;

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