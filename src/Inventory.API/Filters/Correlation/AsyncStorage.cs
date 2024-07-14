namespace Inventory.API.Filters.Correlation;

/// <summary>
/// Stores and retrieves values in an async context.
/// </summary>
/// <typeparam name="T">What should be stored</typeparam>
public static class AsyncStorage<T> where T : new()
{
    private static readonly AsyncLocal<T> _asyncLocal = new AsyncLocal<T>();
    
    public static T Store(T val)
    {
        _asyncLocal.Value = val;
        return _asyncLocal.Value;
    }

    public static T? Retrieve()
    {
        return _asyncLocal.Value;
    }
}