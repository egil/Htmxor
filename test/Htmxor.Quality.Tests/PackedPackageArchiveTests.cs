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

	[Fact]
	public void Packaged_script_does_not_forward_the_legacy_event_handler_id_but_keeps_antiforgery_forwarding()
	{
		using var archive = ZipFile.OpenRead(package.PackagePath);
		var entry = archive.GetEntry("staticwebassets/htmxor.js");
		Assert.NotNull(entry);
		using var reader = new StreamReader(entry.Open());
		var script = reader.ReadToEnd();

		Assert.DoesNotContain("hxor-eventid", script, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("HXOR-Event-Handler-Id", script, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("RequestVerificationToken", script, StringComparison.Ordinal);
	}
}
