namespace BlazorSSR;

public class ContactsRepository
{
    private readonly DiskStorage disk;

    public ContactsRepository(DiskStorage disk)
    {
        this.disk = disk;
    }

    public async Task<IEnumerable<Contact>> All(CancellationToken cancellationToken = default)
    {
        return await disk.Load(cancellationToken);
    }

    public async Task<IEnumerable<Contact>> Search(string query, CancellationToken cancellationToken = default)
    {
        var contacts = await disk.Load(cancellationToken);
        return contacts.Where(c =>
            c.First?.Contains(query, StringComparison.OrdinalIgnoreCase) == true
         || c.Last?.Contains(query, StringComparison.OrdinalIgnoreCase) == true
         || c.Phone?.Contains(query, StringComparison.OrdinalIgnoreCase) == true
         || c.Email?.Contains(query, StringComparison.OrdinalIgnoreCase) == true);
    }

    public async Task<Contact?> Find(int id, CancellationToken cancellationToken = default)
    {
        var contacts = await disk.Load(cancellationToken);
        return contacts.FirstOrDefault(c => c.Id == id);
    }

    public async Task<bool> Save(Contact contact, CancellationToken cancellationToken = default)
    {
        if (!await Validate(contact, cancellationToken))
        {
            return false;
        }

        var contacts = await disk.Load(cancellationToken);

        if (contact.Id == 0)
        {
            contact = contact with { Id = contacts.Max(c => c.Id) + 1 };
            contacts.Add(contact);
        }
        else
        {
            var existingContact = contacts.FirstOrDefault(c => c.Id == contact.Id);
            if (existingContact is not null)
            {
                existingContact.First = contact.First;
                existingContact.Last = contact.Last;
                existingContact.Phone = contact.Phone;
                existingContact.Email = contact.Email;
            }
        }

        await disk.Save(contacts, cancellationToken);
        return true;
    }

    public async Task<bool> Delete(int id, CancellationToken cancellationToken = default)
    {
        var contacts = await disk.Load(cancellationToken);
        if (contacts.RemoveAll(c => c.Id == id) > 0)
        {
            await disk.Save(contacts, cancellationToken);
            return true;
        }

        return false;
    }

    public async Task<bool> Validate(Contact contact, CancellationToken cancellationToken = default)
    {
        var validator = new Contact.Validator(this);
        var result = await validator.ValidateAsync(contact, cancellationToken);
        return result.IsValid;
    }
}
