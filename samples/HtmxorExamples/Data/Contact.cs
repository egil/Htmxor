using System.ComponentModel.DataAnnotations;

namespace HtmxorExamples.Data;

public record class Contact
{
	public required Guid Id { get; set; }

	[Required, StringLength(maximumLength: 100)]
	public required string FirstName { get; set; }

	[Required, StringLength(100)]
	public required string LastName { get; set; }

	[Required, EmailAddress]
	public required string Email { get; set; }

	[Required, Phone]
	public required string PhoneNumber { get; set; }

	[Required, StringLength(maximumLength: 500)]
	public required string Address { get; set; }

	[Required, StringLength(maximumLength: 100)]
	public required string City { get; set; }

	public string? State { get; set; }

	[Required, StringLength(maximumLength: 12)]
	public required string Zip { get; set; }

	[Required, StringLength(maximumLength: 100)]
	public required string Country { get; set; }

	[StringLength(maximumLength: 1000)]
	public string? Notes { get; set; }

	public bool Archived { get; set; }
}
