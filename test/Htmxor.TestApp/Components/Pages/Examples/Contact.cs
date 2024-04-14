namespace Htmxor.TestApp.Components.Pages.Examples;

public record class Contact : IStoreItem
{
    public int Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
}