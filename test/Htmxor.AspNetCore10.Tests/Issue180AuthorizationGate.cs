using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Htmxor.Explicit.Anywhere;

public sealed class Issue180AuthorizationGate
{
    public const string Marker = "issue180-component-only";
    private TaskCompletionSource? entered;
    private TaskCompletionSource? release;

    public Task Entered => entered?.Task ?? throw new InvalidOperationException("The authorization gate was not held.");

    public void Hold()
    {
        entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public void Release() => release?.TrySetResult();

    public async Task WaitForComponentAsync(object? resource)
    {
        if (!string.Equals(resource as string, Marker, StringComparison.Ordinal) || release is null) return;
        entered!.TrySetResult();
        await release.Task;
    }
}

internal sealed class Issue180GatedAuthorizationService(
    IAuthorizationService stock, Issue180AuthorizationGate gate) : IAuthorizationService
{
    public async Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource,
        IEnumerable<IAuthorizationRequirement> requirements)
    {
        await gate.WaitForComponentAsync(resource);
        return await stock.AuthorizeAsync(user, resource, requirements);
    }

    public async Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName)
    {
        await gate.WaitForComponentAsync(resource);
        return await stock.AuthorizeAsync(user, resource, policyName);
    }
}
