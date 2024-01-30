using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Htmxor.Http.Mock;

/// <summary>
/// Mock implementation of the <see cref="IHeaderDictionary"/> interface for testing purposes.
/// </summary>
internal class MockHeaderDictionary : IHeaderDictionary
{
    private readonly Dictionary<string, StringValues> _headers = new Dictionary<string, StringValues>();

    /// <inheritdoc/>
    public StringValues this[string key]
    {
        get => _headers.ContainsKey(key) ? _headers[key] : StringValues.Empty;
        set => _headers[key] = value;
    }

    public long? ContentLength { get; set; } = 0;

    /// <inheritdoc/>
    public int Count => _headers.Count;

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    /// <inheritdoc/>
    public ICollection<string> Keys => _headers.Keys;

    /// <inheritdoc/>
    public ICollection<StringValues> Values => _headers.Values;

    /// <inheritdoc/>
    public void Add(KeyValuePair<string, StringValues> item)
    {
        _headers.Add(item.Key, item.Value);
    }

    /// <inheritdoc/>
    public void Add(string key, StringValues value)
    {
        _headers.Add(key, value);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _headers.Clear();
    }

    /// <inheritdoc/>
    public bool Contains(KeyValuePair<string, StringValues> item)
    {
        return _headers.ContainsKey(item.Key) && _headers[item.Key].Equals(item.Value);
    }

    /// <inheritdoc/>
    public bool ContainsKey(string key)
    {
        return _headers.ContainsKey(key);
    }

    /// <inheritdoc/>
    public void CopyTo(KeyValuePair<string, StringValues>[] array, int arrayIndex)
    {
        foreach (var entry in _headers)
        {
            array[arrayIndex++] = new KeyValuePair<string, StringValues>(entry.Key, entry.Value);
        }
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator()
    {
        return _headers.GetEnumerator();
    }

    /// <inheritdoc/>
    public bool Remove(KeyValuePair<string, StringValues> item)
    {
        return _headers.Remove(item.Key);
    }

    /// <inheritdoc/>
    public bool Remove(string key)
    {
        return _headers.Remove(key);
    }

    /// <inheritdoc/>
    public bool TryGetValue(string key, out StringValues value)
    {
        return _headers.TryGetValue(key, out value);
    }

    /// <inheritdoc/>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

