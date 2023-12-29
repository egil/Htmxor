using BlazorSSR.Components.FlashMessages;

namespace BlazorSSR.Components.Contacts;

public static class DeleteContactEndpoint
{
    public static void MapContactsDelete(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost(
            "/contacts/{id}/delete",
            async (int id, ContactsRepository repo, FlashMessageQueue flashMessage, HttpContext context, CancellationToken cancellationToken) =>
        {
            if (await repo.Delete(id, cancellationToken))
            {
                flashMessage.Add("Deleted contact!", FlashMessageType.Success);
                context.Response.Redirect("/contacts", permanent: false);
            }
            else
            {
                flashMessage.Add("Failed to delete contact!", FlashMessageType.Error);
                context.Response.Redirect($"/contacts/{id}/edit", permanent: false);
            }
        });
}
