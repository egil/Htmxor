using System.Text.Json;

namespace BlazorSSR;

public sealed class DiskStorage
{
    private const string ContactsFileName = "contacts.json";
    private readonly JsonSerializerOptions contactsFileJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private List<Contact>? loadedContacts = default;

    public async Task Save(IReadOnlyList<Contact> contacts, CancellationToken cancellationToken)
    {
        using var stream = File.Open(ContactsFileName, FileMode.Create);
        await JsonSerializer.SerializeAsync(
            stream,
            contacts.OrderBy(x => x.Id),
            contactsFileJsonOptions,
            cancellationToken);
        loadedContacts = null;
    }

    public async Task<List<Contact>> Load(CancellationToken cancellationToken)
    {
        if (loadedContacts is not null)
            return loadedContacts;

        using var stream = File.OpenRead(ContactsFileName);
        loadedContacts = await JsonSerializer.DeserializeAsync<List<Contact>>(stream, contactsFileJsonOptions, cancellationToken);

        return loadedContacts ?? new();
    }
}
