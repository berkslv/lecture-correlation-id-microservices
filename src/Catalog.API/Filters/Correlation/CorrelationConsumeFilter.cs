using MassTransit;
using Serilog.Events;
using LogContext = Serilog.Context.LogContext;

namespace Catalog.API.Filters.Correlation;

/// <summary>
/// It is triggered when there is an event consumed by MassTransit and sets the CorrelationId value to the Correlation class.
/// </summary>
public class CorrelationConsumeFilter<T> : IFilter<ConsumeContext<T>> where T : class
{
    public Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        var correlationIdHeader = context.CorrelationId;

        if (correlationIdHeader.HasValue)
        {
            var correlationId = correlationIdHeader.Value;
            
            LogContext.PushProperty("CorrelationId", new ScalarValue(correlationId));
            
            AsyncStorage<Correlation>.Store(new Correlation
            {
                Id = correlationId
            });
        }

        return next.Send(context);
    }

    public void Probe(ProbeContext context)
    {
    }
}