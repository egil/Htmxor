using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Htmxor.Samples.Tests;

// Thread-pool limits are process-wide; this collection must not overlap any other collection.
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SampleReadinessCollection
{
	public const string Name = "Sample readiness under starvation";
}

[Collection(SampleReadinessCollection.Name)]
public sealed class SampleReadinessStarvationTests(ITestOutputHelper output)
{
	private const string Project = "samples/MinimalHtmxorApp/MinimalHtmxorApp.csproj";

	[Fact]
	public void Serving_sample_returns_a_host_while_the_parent_pool_is_saturated()
	{
		OnDedicatedThread(() =>
		{
			var port = FindAvailablePort();
			using var child = new SampleProcessLease(blockStartup: false);
			using var pool = new SaturatedThreadPool();
			pool.Saturate();
			var elapsed = Stopwatch.StartNew();
			var environment = child.Environment;
			environment["Kestrel__Endpoints__Readiness__Url"] = $"http://127.0.0.1:{port}";
			var starting = SampleKestrelHost.StartAsync(Project, environment);
			CheckAndCleanUp(() =>
			{
				Assert.True(SpinWait.SpinUntil(() => IsServing(port), TimeSpan.FromSeconds(10)), "Child sample did not serve HTTP independently of the parent pool.");
				output.WriteLine("Child HTTP 200 at {0:F3}s; host task {1}; pool saturated={2}.", elapsed.Elapsed.TotalSeconds, starting.Status, SaturatedThreadPool.IsSaturated());
				Assert.True(SaturatedThreadPool.IsSaturated());
				var remaining = TimeSpan.FromSeconds(30) - elapsed.Elapsed;
				Assert.True(SpinWait.SpinUntil(() => starting.IsCompleted, remaining), "Child serves HTTP 200, but the returned host task remains pending through the existing 30s readiness deadline while the parent pool is saturated.");
				Assert.True(starting.IsCompletedSuccessfully, starting.Exception?.ToString());
			}, () => ReleaseAndCleanUp(pool, starting, elapsed, child));
		});
	}

	[Fact]
	public void Unready_sample_times_out_with_diagnostics_while_the_parent_pool_is_saturated()
	{
		OnDedicatedThread(() =>
		{
			using var child = new SampleProcessLease(blockStartup: true);
			using var pool = new SaturatedThreadPool();
			pool.Saturate();
			var elapsed = Stopwatch.StartNew();
			var starting = SampleKestrelHost.StartAsync(Project, child.Environment);
			CheckAndCleanUp(() =>
			{
				Assert.True(SpinWait.SpinUntil(() => File.Exists(child.MarkerPath), TimeSpan.FromSeconds(10)), "Child did not reach its blocking startup hook.");
				Assert.True(SaturatedThreadPool.IsSaturated());
				output.WriteLine("Child blocked before entry point at {0:F3}s; host task {1}.", elapsed.Elapsed.TotalSeconds, starting.Status);
				Assert.True(SpinWait.SpinUntil(() => starting.IsCompleted, TimeSpan.FromSeconds(35) - elapsed.Elapsed), "The returned host task did not time out independently of the saturated parent pool (30s host deadline plus 5s observation watchdog).");
				Assert.InRange(elapsed.Elapsed, TimeSpan.FromSeconds(29), TimeSpan.FromSeconds(35));
				var failure = Assert.IsType<TimeoutException>(starting.Exception?.InnerException);
				AssertTimeoutDiagnostics(failure.Message);
			}, () => ReleaseAndCleanUp(pool, starting, elapsed, child));
		});
	}

	[Fact]
	public void Permanently_pending_host_cleanup_reaps_its_child_and_preserves_the_primary_failure()
	{
		OnDedicatedThread(() =>
		{
			using var child = new SampleProcessLease(blockStartup: true);
			using var pool = new SaturatedThreadPool();
			pool.Saturate();
			var elapsed = Stopwatch.StartNew();
			var realStart = SampleKestrelHost.StartAsync(Project, child.Environment);
			var neverCompletes = new TaskCompletionSource<SampleKestrelHost>().Task;
			var primary = new InvalidOperationException("Primary behavioral failure sentinel.");
			var observed = Assert.Throws<InvalidOperationException>(() => CheckAndCleanUp(() =>
			{
				Assert.True(SpinWait.SpinUntil(() => File.Exists(child.MarkerPath), TimeSpan.FromSeconds(10)), "Child did not record its independent identity.");
				throw primary;
			}, () => ReleaseAndCleanUp(pool, neverCompletes, elapsed, child)));
			Assert.Same(primary, observed);
			Assert.False(File.Exists(child.MarkerPath));
			Assert.True(SpinWait.SpinUntil(() => realStart.IsCompleted, TimeSpan.FromSeconds(5)), "Real child cleanup did not complete independently of the substituted pending task.");
			Assert.True(realStart.IsFaulted);
			output.WriteLine("Pending-task cleanup probe at {0:F3}s: marker deleted, real child reaped, primary exception preserved; real host error={1}.", elapsed.Elapsed.TotalSeconds, realStart.Exception);
		});
	}

	private void ReleaseAndCleanUp(SaturatedThreadPool pool, Task<SampleKestrelHost> starting, Stopwatch elapsed, SampleProcessLease child)
	{
		try
		{
			ReleaseAndDisposeHost(pool, starting, elapsed);
		}
		finally
		{
			child.Dispose();
		}
	}

	private void ReleaseAndDisposeHost(SaturatedThreadPool pool, Task<SampleKestrelHost> starting, Stopwatch elapsed)
	{
		output.WriteLine("Before pool release at {0:F3}s: host task {1}; pending work={2}.", elapsed.Elapsed.TotalSeconds, starting.Status, ThreadPool.PendingWorkItemCount);
		pool.Dispose();
		Assert.True(SpinWait.SpinUntil(() => starting.IsCompleted, TimeSpan.FromSeconds(10)), "Host task did not finish even after pool release.");
		output.WriteLine("After pool release at {0:F3}s: host task {1}; error={2}.", elapsed.Elapsed.TotalSeconds, starting.Status, starting.Exception?.InnerException?.Message);
		if (starting.IsCompletedSuccessfully)
		{
			starting.Result.DisposeAsync().AsTask().GetAwaiter().GetResult();
		}
	}

	private void CheckAndCleanUp(Action assertions, Action cleanup)
	{
		ExceptionDispatchInfo? primary = null;
		try
		{
			assertions();
		}
		catch (Exception exception)
		{
			primary = ExceptionDispatchInfo.Capture(exception);
		}

		try
		{
			cleanup();
		}
		catch (Exception exception) when (primary is not null)
		{
			output.WriteLine("Cleanup also failed; preserving the primary assertion: {0}", exception);
		}

		primary?.Throw();
	}

	private static void AssertTimeoutDiagnostics(string message)
	{
		Assert.Matches(@"after \d+\.\d{3}s", message);
		Assert.Matches(@"Process \d+: running", message);
		Assert.Contains("readiness limit: 30s", message, StringComparison.Ordinal);
		Assert.Contains("running", message, StringComparison.Ordinal);
		Assert.Contains("ThreadPool.ThreadCount=", message, StringComparison.Ordinal);
		Assert.Contains("PendingWorkItemCount=", message, StringComparison.Ordinal);
		Assert.Contains("CompletedWorkItemCount=", message, StringComparison.Ordinal);
		Assert.Contains("Sample startup blocked before application entry point.", message, StringComparison.Ordinal);
		Assert.Contains("Sample startup stderr marker.", message, StringComparison.Ordinal);
		Assert.Contains("+", message, StringComparison.Ordinal);
	}

	private static int FindAvailablePort()
	{
		using var listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		return ((IPEndPoint)listener.LocalEndpoint).Port;
	}

	private static bool IsServing(int port)
	{
		try
		{
			using var client = new TcpClient { ReceiveTimeout = 1000, SendTimeout = 1000 };
			client.Connect(IPAddress.Loopback, port);
			using var stream = client.GetStream();
			stream.Write("GET / HTTP/1.0\r\nHost: localhost\r\n\r\n"u8);
			using var reader = new StreamReader(stream, Encoding.UTF8);
			return reader.ReadLine()?.Contains("200 OK", StringComparison.Ordinal) == true
				&& reader.ReadToEnd().Contains("Welcome to your new app.", StringComparison.Ordinal);
		}
		catch (SocketException)
		{
			return false;
		}
		catch (IOException)
		{
			return false;
		}
	}

	private static void OnDedicatedThread(Action scenario)
	{
		ExceptionDispatchInfo? failure = null;
		var worker = new Thread(() =>
		{
			try
			{
				scenario();
			}
			catch (Exception exception)
			{
				failure = ExceptionDispatchInfo.Capture(exception);
			}
		}) { IsBackground = true, Name = "Sample readiness starvation controller" };
		worker.Start();
		Assert.True(worker.Join(TimeSpan.FromSeconds(100)), "Dedicated test controller exceeded its outer watchdog.");
		failure?.Throw();
	}
}
