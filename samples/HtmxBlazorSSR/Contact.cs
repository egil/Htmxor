using FluentValidation;

namespace HtmxBlazorSSR;

public record class Contact
{
    public int Id { get; init; }
    public string? First { get; set; }
    public string? Last { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    public sealed class Validator : AbstractValidator<Contact>
    {
        public Validator(ContactsRepository repo)
        {
            RuleFor(c => c.Email)
                .NotEmpty()
                .EmailAddress()
                .MustAsync(async (email, cancellationToken) =>
                {
                    var contacts = await repo.All(cancellationToken);
                    return !contacts.Any(c => c.Email == email);
                })
                .WithMessage("Email must be unique");
        }
    }
}

