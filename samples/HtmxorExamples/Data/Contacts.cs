using System.Collections.Concurrent;
using Bogus;

namespace HtmxorExamples.Data;

public static class Contacts
{
	public static SortedDictionary<Guid, Contact> Data { get; } = GenerateContacts();

	private static SortedDictionary<Guid, Contact> GenerateContacts()
	{
		var fakes = new Faker<Contact>()
			.UseSeed(486967)
			.RuleFor(c => c.Id, f => f.Random.Guid())
			.RuleFor(c => c.FirstName, f => f.Person.FirstName)
			.RuleFor(c => c.LastName, f => f.Person.LastName)
			.RuleFor(c => c.Email, f => f.Person.Email)
			.RuleFor(c => c.PhoneNumber, f => f.Person.Phone)
			.RuleFor(c => c.Address, f => f.Address.StreetAddress())
			.RuleFor(c => c.City, f => f.Address.City())
			.RuleFor(c => c.State, f => f.Address.State())
			.RuleFor(c => c.Zip, f => f.Address.ZipCode())
			.RuleFor(c => c.Country, f => f.Address.Country())
			.RuleFor(c => c.Notes, f => f.Lorem.Sentence())
			.RuleFor(c => c.Archived, f => f.Random.Bool())
			.RuleFor(c => c.Modified, f => f.Date.Past(yearsToGoBack: 1))
			.Generate(1000);

		return new(fakes.ToDictionary(c => c.Id, c => c));
	}
}
