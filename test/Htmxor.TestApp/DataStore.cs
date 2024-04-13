using System.Collections.Concurrent;
using Htmxor.TestApp.Components.Pages.ClickToEdit1;

namespace Htmxor.TestApp;

public class DataStore
{
    private ConcurrentDictionary<(int Id, Type Type), object?> data = new();
    
    public int GetNextId<T>() where T : IStoreItem
        => data.Keys
            .Where(x => x.Type == typeof(T))
            .Select(x => x.Id)
            .DefaultIfEmpty(0)
            .Max() + 1;

    public T? GetOrDefault<T>(int id)
        where T : IStoreItem
    {
        return data.TryGetValue((id, typeof(T)), out var value) && value is T result
            ? result
            : default(T);
    }

    public void Store<T>(T value)
        where T : IStoreItem
        => data[(value.Id, typeof(T))] = value;
}

public interface IStoreItem
{
    int Id { get; }
}
