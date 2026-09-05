namespace Htmxor.Quality;

internal static class Program
{
	public static async Task<int> Main(string[] args)
	{
		try
		{
			var options = QualityOptions.Parse(args);
			var repositoryRoot = RepositoryLocator.Find();
			var command = new QualityCommand(repositoryRoot, new ProcessRunner());
			await command.ExecuteAsync(options);
			return 0;
		}
		catch (Exception exception)
		{
			Console.Error.WriteLine(exception.Message);
			return exception.Data["ExitCode"] is int exitCode ? exitCode : 1;
		}
	}
}
