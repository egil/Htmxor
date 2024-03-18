using System.Diagnostics.CodeAnalysis;

namespace Htmxor.TestAssets;

public sealed class TestAppFixture : IAsyncLifetime
{
    [NotNull]
    public IAlbaHost Host { get; private set; } = default!;

    public async Task DisposeAsync()
    {
        await Host.DisposeAsync();
    }

    public async Task InitializeAsync()
    {
        Host = await AlbaHost.For<global::Program>();
    }
}
