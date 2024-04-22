using System.ComponentModel.DataAnnotations;

namespace Htmxor.TestApp.Components.Pages.Examples;

public record class Contact : IStoreItem
{
    public int Id { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "First name is required"), MinLength(1)]
    public string? FirstName { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "Last name is required"), MinLength(1)]
    public string? LastName { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "Email is required"), MinLength(1)]
    public string? Email { get; set; }
}