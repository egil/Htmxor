using System.Collections.Concurrent;
using Htmxor.TestApp.Components.Pages.Examples;

namespace Htmxor.TestApp;

public static class DataStore
{
    private readonly static ConcurrentDictionary<(int Id, Type Type), object?> data = new();

    static DataStore()
    {
        data.TryAdd((1, typeof(Contact)), new Contact { Id = 1, FirstName = "Joe", LastName = "Blow", Email = "joe@blow.com" });

        data.TryAdd((1, typeof(User)), new User { Id = 1, Name = "Alice", Email = "alice@example.com", Active = true });
        data.TryAdd((2, typeof(User)), new User { Id = 2, Name = "Bob", Email = "bob@example.com", Active = true });
        data.TryAdd((3, typeof(User)), new User { Id = 3, Name = "Charlie", Email = "charlie@example.com", Active = true });
        data.TryAdd((4, typeof(User)), new User { Id = 4, Name = "Dave", Email = "dave@example.com", Active = true });
    }

    public static int GetNextId<T>() where T : IStoreItem
        => data.Keys
            .Where(x => x.Type == typeof(T))
            .Select(x => x.Id)
            .DefaultIfEmpty(0)
            .Max() + 1;

    public static T? GetOrDefault<T>(int id)
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

    public static void Store<T>(T value)
        where T : IStoreItem
        => data[(value.Id, typeof(T))] = value;
}

public interface IStoreItem
{
    int Id { get; }
}
