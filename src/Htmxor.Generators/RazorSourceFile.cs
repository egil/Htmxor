using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Htmxor.Generators;

internal sealed class RazorSourceFile
{
	private RazorSourceFile(string path, SourceText source)
	{
		Path = path;
		Source = source;
	}

	public string Path { get; }

	public SourceText Source { get; }

	public static RazorSourceFile Read(AdditionalText file, CancellationToken cancellationToken)
		=> new(
			file.Path,
			file.GetText(cancellationToken) ?? SourceText.From(string.Empty, Encoding.UTF8));
}
