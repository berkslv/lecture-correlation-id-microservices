using Catalog.API.Services.Interfaces;


namespace Catalog.API.Services;

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