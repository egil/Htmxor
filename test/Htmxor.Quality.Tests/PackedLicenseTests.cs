using System.IO.Compression;
using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

[Collection(PackageConsumerCollection.Name)]
public sealed class PackedLicenseTests
{
	[Fact]
	public async Task Package_contains_the_unchanged_ASPNET_Core_license()
	{
		var repositoryRoot = RepositoryLocator.Find();
		using var workspace = new PackageConsumerWorkspace(repositoryRoot);
		await workspace.PackOnlyAsync();

		using var package = ZipFile.OpenRead(workspace.PackagePath);
		var license = package.GetEntry("licenses/ASP.NET-Core-LICENSE.txt");
		Assert.NotNull(license);
		using var licenseStream = license.Open();
		using var packedBytes = new MemoryStream();
		await licenseStream.CopyToAsync(packedBytes);

		Assert.Equal(
			await File.ReadAllBytesAsync(Path.Combine(repositoryRoot, "src", "Htmxor", "Endpoints", "ASP.NET-Core-LICENSE.txt")),
			packedBytes.ToArray());
	}
}
