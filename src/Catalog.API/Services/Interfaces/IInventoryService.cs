namespace Catalog.API.Services.Interfaces;

public interface IInventoryService
{
    public Task RemoveStockAsync(string productId, int quantity);
    
}