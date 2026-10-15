using System.Net;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.AspNetCore10;

public sealed class Issue260AbandonedNavigationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Pending_navigation_drains_queued_work_and_discards_the_abandoned_tree(bool htmxor)
    {
        await using var host = await Issue260Host.CreateAsync<Issue260App>(htmxor);
        var gate = host.Services.GetRequiredService<Issue260Gate>();
        var responseTask = host.Client.GetAsync("/issue-329/abandoned-navigation");
        try
        {
            await gate.WaitForReachedAsync("abandoned-parent");
            gate.Release("abandoned-parent");
            await gate.WaitForReachedAsync("queued-before-navigation");
            await gate.WaitForReachedAsync("navigation-during-diff");
            // The queued child is discovered during the abandoned diff, after the first pending snapshot.
            await Assert.ThrowsAsync<TimeoutException>(() => responseTask.WaitAsync(TimeSpan.FromMilliseconds(300)));
            gate.Release("queued-before-navigation");
            using var response = await responseTask.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(gate.WaitForReachedAsync("queued-work-completed").IsCompletedSuccessfully);
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.EndsWith("/issue-260/destination", response.Headers.Location?.ToString(), StringComparison.Ordinal);
            Assert.Empty(await response.Content.ReadAsStringAsync());
        }
        finally
        {
            gate.Release("abandoned-parent");
            gate.Release("queued-before-navigation");
        }
    }
}
