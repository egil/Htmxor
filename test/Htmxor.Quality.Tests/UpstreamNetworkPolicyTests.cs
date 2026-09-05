using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

public sealed class UpstreamNetworkPolicyTests
{
	[Fact]
	public void Unclassified_process_capability_is_unknown_and_rejected_from_ordinary_profiles()
	{
		var command = new ProcessCommand("dotnet", "/repo", ["run", "--project", "new-tool.csproj"]);

		Assert.Equal(NetworkAccess.Unknown, command.NetworkAccess);
		Assert.False(UpstreamMonitorPolicyTests.IsOrdinaryCommand(command));
	}

	[Theory]
	[InlineData("Disabled")]
	[InlineData("Enabled")]
	[InlineData("Unknown")]
	public void Inserted_monitor_is_rejected_even_when_its_capability_is_mislabeled(string capability)
	{
		var command = new ProcessCommand("dotnet", "/repo",
			["run", "--project", "/repo/eng/Htmxor.UpstreamMonitor/Htmxor.UpstreamMonitor.csproj"], NetworkAccess: Enum.Parse<NetworkAccess>(capability));

		Assert.False(UpstreamMonitorPolicyTests.IsOrdinaryCommand(command));
	}
}
