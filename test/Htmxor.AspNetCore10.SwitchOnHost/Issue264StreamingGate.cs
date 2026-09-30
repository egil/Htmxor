namespace Htmxor.AspNetCore10.SwitchOnHost;

// Lets the test control exactly when Issue264StreamingPage navigates and when it resumes afterward, instead
// of guessing with fixed sleeps. Only one streaming request is ever in flight at a time in this test suite
// (xUnit runs a class's [Fact]/[Theory] methods sequentially), so one process-wide instance, reset per
// request, is sufficient; it is not a general-purpose per-request registry.
internal sealed class Issue264StreamingGate
{
	private TaskCompletionSource navigate = CreateSource();
	private TaskCompletionSource resume = CreateSource();

	public Task NavigateReleased => navigate.Task;

	public Task ResumeReleased => resume.Task;

	public void Reset()
	{
		navigate = CreateSource();
		resume = CreateSource();
	}

	public void ReleaseNavigate() => navigate.TrySetResult();

	public void ReleaseResume() => resume.TrySetResult();

	private static TaskCompletionSource CreateSource() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
