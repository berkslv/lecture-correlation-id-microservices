using Serilog.Context;
using Serilog.Events;

namespace Inventory.API.Filters.Correlation;

/// <summary>
/// When the Http request is made, it takes the CorrelationId value from the HttpContext Header and sets the Correlation.Id value.
/// </summary>
public class CorrelationMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, Correlation correlation)
    {
        var correlationIdHeader = context.Request.Headers["CorrelationId"];

        if (!string.IsNullOrWhiteSpace(correlationIdHeader))
        {
            var correlationId = Guid.Parse(correlationIdHeader.ToString());

            LogContext.PushProperty("CorrelationId", new ScalarValue(correlationId));
            
            AsyncStorage<Correlation>.Store(new Correlation
            {
                Id = correlationId
            });
        }

        await _next(context);
    }
}