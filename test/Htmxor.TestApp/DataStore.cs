using System.Collections.Concurrent;
using Htmxor.TestApp.Components.Pages.Examples;

namespace Htmxor.TestApp;

public static class DataStore
{
    private readonly static ConcurrentDictionary<(Guid Id, Type Type), object?> data = new();

    static DataStore()
    {
    }

    public static T? GetOrDefault<T>(Guid id)
        where T : IStoreItem
    {
        return data.TryGetValue((id, typeof(T)), out var value) && value is T result
            ? result
            : default(T);
    }

    public static IEnumerable<T> OfType<T>()
    {
        return data.Values.OfType<T>();
    }

    public static T Store<T>(T value)
        where T : IStoreItem
    {
        data[(value.Id, typeof(T))] = value;
        return value;
    }

    public static void Remove<T>(Guid id)
        where T : IStoreItem
        => data.TryRemove((id, typeof(T)), out _);
}

public interface IStoreItem
{
    Guid Id { get; }
}
