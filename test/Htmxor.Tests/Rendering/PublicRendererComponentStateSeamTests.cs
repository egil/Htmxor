using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.HtmlRendering.Infrastructure;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace Htmxor.Rendering;

public sealed class PublicRendererComponentStateSeamTests
{
	private readonly ITestOutputHelper output;

	public PublicRendererComponentStateSeamTests(ITestOutputHelper output) => this.output = output;

	[Fact]
	public async Task Completed_nested_component_boundary_can_be_serialized_without_root_or_sibling()
	{
		var probe = new RenderProbe();
		await using var services = new ServiceCollection().AddSingleton(probe).BuildServiceProvider();
		await using var renderer = new ProbeRenderer(services);
		await renderer.Dispatcher.InvokeAsync(async () =>
		{
			var root = renderer.BeginRenderingComponent(typeof(NestedBoundaryRoot), ParameterView.Empty);
			await probe.SelectedInitializationEntered.Task;
			root.QuiescenceTask.IsCompleted.Should().BeFalse();
			probe.ReleaseSelectedInitialization();
			await root.QuiescenceTask;
			var completedEvents = probe.Events.ToArray();
			var rootMarkup = renderer.WriteRootComponentHtml();
			var selectedMarkup = renderer.WriteSelectedComponentHtml();

			rootMarkup.Should().Be("<main data-root=\"\"><div><section data-selected=\"\">selected:ready</section></div><aside data-root-sibling=\"\">sibling</aside></main>");
			rootMarkup.Should().Contain("data-root");
			rootMarkup.Should().Contain("data-root-sibling");
			selectedMarkup.Should().Be("<section data-selected=\"\">selected:ready</section>");
			selectedMarkup.Should().NotContain("data-root");
			selectedMarkup.Should().NotContain("data-root-sibling");
			completedEvents.Count(eventName => eventName == "lifecycle:root").Should().Be(1);
			completedEvents.Count(eventName => eventName == "lifecycle:container").Should().Be(1);
			completedEvents.Count(eventName => eventName == "lifecycle:selected").Should().Be(1);
			completedEvents.Count(eventName => eventName == "lifecycle:sibling").Should().Be(1);
			completedEvents.Count(eventName => eventName == "data-ready:selected").Should().Be(1);
			completedEvents.Should().Contain("data:selected");
			completedEvents.Should().Contain("data:sibling");
			output.WriteLine($"Selected component output: {selectedMarkup}");
			output.WriteLine($"Completed lifecycle and data work: {string.Join(", ", probe.Events)}");
		});
	}

	private sealed class ProbeRenderer : StaticHtmlRenderer
	{
		private int rootComponentId = -1;
		private int selectedComponentId = -1;
		public ProbeRenderer(IServiceProvider services) : base(services, NullLoggerFactory.Instance) { }
		protected override ComponentState CreateComponentState(int componentId, IComponent component, ComponentState? parentComponentState)
		{
			if (component is NestedBoundaryRoot) rootComponentId = componentId;
			if (component is SelectedBoundary) selectedComponentId = componentId;
			return base.CreateComponentState(componentId, component, parentComponentState);
		}
		public string WriteRootComponentHtml() => WriteComponentHtml(rootComponentId);
		public string WriteSelectedComponentHtml() => WriteComponentHtml(selectedComponentId);
		private string WriteComponentHtml(int componentId)
		{
			componentId.Should().NotBe(-1);
			using var writer = new StringWriter();
			WriteComponentHtml(componentId, writer);
			return writer.ToString();
		}
	}

	private sealed class NestedBoundaryRoot : ObservedComponent
	{
		protected override string Name => "root";
		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenElement(0, "main"); builder.AddAttribute(1, "data-root", "");
			builder.OpenComponent<NestedBoundaryContainer>(2); builder.CloseComponent();
			builder.OpenComponent<RootSibling>(3); builder.CloseComponent(); builder.CloseElement();
		}
	}
	private sealed class NestedBoundaryContainer : ObservedComponent
	{
		protected override string Name => "container";
		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenElement(0, "div"); builder.OpenComponent<SelectedBoundary>(1); builder.CloseComponent(); builder.CloseElement();
		}
	}
	private sealed class SelectedBoundary : ObservedComponent
	{
		protected override string Name => "selected";
		protected override async Task OnInitializedAsync()
		{
			await base.OnInitializedAsync();
			await Probe.WaitForSelectedInitializationAsync();
			Probe.RecordDataReady(Name);
		}
		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenElement(0, "section"); builder.AddAttribute(1, "data-selected", ""); builder.AddContent(2, Probe.Read(Name)); builder.AddContent(3, ":ready"); builder.CloseElement();
		}
	}
	private sealed class RootSibling : ObservedComponent
	{
		protected override string Name => "sibling";
		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenElement(0, "aside"); builder.AddAttribute(1, "data-root-sibling", ""); builder.AddContent(2, Probe.Read(Name)); builder.CloseElement();
		}
	}
	private abstract class ObservedComponent : ComponentBase
	{
		[Inject] protected RenderProbe Probe { get; set; } = default!;
		protected abstract string Name { get; }
		protected override void OnInitialized() => Probe.RecordLifecycle(Name);
	}
	private sealed class RenderProbe
	{
		public TaskCompletionSource SelectedInitializationEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private readonly TaskCompletionSource selectedInitializationRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public List<string> Events { get; } = [];
		public string Read(string component) { Events.Add($"data:{component}"); return component; }
		public void RecordLifecycle(string component) => Events.Add($"lifecycle:{component}");
		public void RecordDataReady(string component) => Events.Add($"data-ready:{component}");
		public async Task WaitForSelectedInitializationAsync()
		{
			SelectedInitializationEntered.TrySetResult();
			await selectedInitializationRelease.Task;
		}
		public void ReleaseSelectedInitialization() => selectedInitializationRelease.TrySetResult();
	}
}
