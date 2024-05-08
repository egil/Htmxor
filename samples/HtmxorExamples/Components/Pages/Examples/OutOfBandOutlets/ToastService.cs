namespace HtmxorExamples.Components.Pages.Examples.OutOfBandOutlets;

public sealed class ToastService
{
	private List<string>? messages;

	internal Action? OnMessagesChanged { get; set; }

	public IReadOnlyList<string> Messages
		=> messages as IReadOnlyList<string> ?? Array.Empty<string>();

	public void AddToast(string message)
	{
		messages ??= new();
		messages.Add(message);
		OnMessagesChanged?.Invoke();
	}
}
