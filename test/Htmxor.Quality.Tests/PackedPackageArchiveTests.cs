using System.IO.Compression;

namespace Htmxor.Quality.Tests;

[Collection(PackedConsumerCollection.Name)]
public sealed class PackedPackageArchiveTests(PackedPackageFixture package)
{
	[Theory]
	[InlineData("LICENSE", "LICENSE")]
	[InlineData("licenses/ASP.NET-Core-LICENSE.txt", "src/Htmxor/Endpoints/ASP.NET-Core-LICENSE.txt")]
	public void Package_ships_the_exact_license_input(string archivePath, string sourcePath)
	{
		using var archive = ZipFile.OpenRead(package.PackagePath);
		var entry = archive.GetEntry(archivePath);
		Assert.NotNull(entry);
		using var stream = entry.Open();
		using var content = new MemoryStream();
		stream.CopyTo(content);

		Assert.Equal(File.ReadAllBytes(Path.Combine(RepositoryLocator.Find(), sourcePath)), content.ToArray());
	}
}
