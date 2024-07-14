using Catalog.API.Services.Interfaces;
using Contracts;
using MassTransit;

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
