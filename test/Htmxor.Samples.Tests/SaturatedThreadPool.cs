using Xunit;

namespace Htmxor.Samples.Tests;

internal sealed class SaturatedThreadPool : IDisposable
{
	private readonly ManualResetEventSlim release = new();
	private readonly CountdownEvent finished = new(1);
	private readonly int minimumWorkers;
	private readonly int minimumIo;
	private readonly int maximumWorkers;
	private readonly int maximumIo;
	private bool disposed;

	public SaturatedThreadPool()
	{
		ThreadPool.GetMinThreads(out minimumWorkers, out minimumIo);
		ThreadPool.GetMaxThreads(out maximumWorkers, out maximumIo);
	}

	public void Saturate()
	{
		try
		{
			Assert.True(ThreadPool.SetMinThreads(1, minimumIo));
			Assert.True(ThreadPool.SetMaxThreads(4, maximumIo));
			for (var index = 0; index < 4; index++)
			{
				finished.AddCount();
				ThreadPool.QueueUserWorkItem(_ => BlockWorker());
			}

			Assert.True(SpinWait.SpinUntil(IsSaturated, TimeSpan.FromSeconds(10)), "Could not saturate the test process thread pool.");
		}
		finally
		{
			finished.Signal();
		}
	}

	public static bool IsSaturated()
	{
		ThreadPool.GetAvailableThreads(out var workers, out _);
		return workers == 0;
	}

	private void BlockWorker()
	{
		try
		{
			release.Wait(TimeSpan.FromSeconds(90));
		}
		finally
		{
			finished.Signal();
		}
	}

	public void Dispose()
	{
		if (disposed)
		{
			return;
		}

		disposed = true;
		release.Set();
		Assert.True(ThreadPool.SetMaxThreads(maximumWorkers, maximumIo));
		Assert.True(ThreadPool.SetMinThreads(minimumWorkers, minimumIo));
		Assert.True(finished.Wait(TimeSpan.FromSeconds(10)), "Pool blockers did not finish after release.");
		release.Dispose();
		finished.Dispose();
	}
}
