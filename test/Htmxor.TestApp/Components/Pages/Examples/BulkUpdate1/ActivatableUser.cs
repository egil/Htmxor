namespace Htmxor.TestApp.Components.Pages.Examples.BulkUpdate1;

public record class ActivatableUser : IStoreItem
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public bool Active { get; set; }
}