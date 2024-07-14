namespace Contracts;

public class OrderCreatedEvent
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}