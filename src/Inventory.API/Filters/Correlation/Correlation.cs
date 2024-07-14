namespace Inventory.API.Filters.Correlation;

/// <summary>
/// It holds the CorrelationId value that comes with HTTP requests and events handled via MassTransit.
/// </summary>
public class Correlation
{
    public Guid Id { get; init; }
}
