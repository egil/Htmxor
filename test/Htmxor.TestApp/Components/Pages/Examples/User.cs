namespace Htmxor.TestApp.Components.Pages.Examples;

public record class User : IStoreItem
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public bool Active { get; set; }
}
