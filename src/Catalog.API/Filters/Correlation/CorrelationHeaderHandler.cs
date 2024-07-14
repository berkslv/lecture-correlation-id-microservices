namespace Catalog.API.Filters.Correlation;

/// <summary>
/// Middleware to be used in requests made with HttpClient. Adds the CorrelationId header to the requests made.
/// </summary>
public class CorrelationHeaderHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlation = AsyncStorage<Correlation>.Retrieve();
        
        if (correlation is not null)
        {
            request.Headers.Add("CorrelationId", correlation.Id.ToString());
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
