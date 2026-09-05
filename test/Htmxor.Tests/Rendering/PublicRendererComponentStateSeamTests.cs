using Htmxor.Endpoints;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.HtmlRendering.Infrastructure;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
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
		var topology = new CompletedTopologyProbe();
		await using var services = new ServiceCollection().AddSingleton(probe).BuildServiceProvider();
		await using var renderer = new ProbeRenderer(services, topology);
		await renderer.Dispatcher.InvokeAsync(async () =>
		{
			var root = renderer.BeginRenderingComponent(typeof(NestedBoundaryRoot), ParameterView.Empty);
			await probe.SelectedInitializationEntered.Task;
			root.QuiescenceTask.IsCompleted.Should().BeFalse();
			probe.ReleaseSelectedInitialization();
			await root.QuiescenceTask;
			var selectedComponentId = topology.ResolveSelectedComponentId();
			var rootMarkup = renderer.WriteRootComponentHtml();
			var selectedMarkup = renderer.WriteSelectedComponentHtml(selectedComponentId);

			rootMarkup.Should().Be("<main data-root=\"\"><div><section data-selected=\"\">selected:ready</section></div><aside data-root-sibling=\"\">sibling</aside></main>");
			rootMarkup.Should().Contain("data-root");
			rootMarkup.Should().Contain("data-root-sibling");
			selectedMarkup.Should().Be("<section data-selected=\"\">selected:ready</section>");
			selectedMarkup.Should().NotContain("data-root");
			selectedMarkup.Should().NotContain("data-root-sibling");
			var completedEvents = probe.Events.ToArray();
			completedEvents.Count(eventName => eventName == "lifecycle:root").Should().Be(1);
			completedEvents.Count(eventName => eventName == "lifecycle:container").Should().Be(1);
			completedEvents.Count(eventName => eventName == "lifecycle:selected").Should().Be(1);
			completedEvents.Count(eventName => eventName == "lifecycle:sibling").Should().Be(1);
			completedEvents.Count(eventName => eventName == "data-ready:selected").Should().Be(1);
			completedEvents.Count(eventName => eventName == "data:selected").Should().Be(2);
			completedEvents.Count(eventName => eventName == "data:sibling").Should().Be(1);
			output.WriteLine($"Selected component output: {selectedMarkup}");
			output.WriteLine($"Completed lifecycle and data work: {string.Join(", ", probe.Events)}");
		});
	}

	[Fact]
	public async Task Production_renderer_writes_the_root_or_one_completed_component_boundary()
	{
		var probe = new RenderProbe();
		var topology = new CompletedTopologyProbe();
		var serviceCollection = new ServiceCollection().AddSingleton(probe);
		await using var services = serviceCollection.BuildServiceProvider();
		await using var renderer = new ProductionProbeRenderer(services, topology);
		var renderTask = renderer.Dispatcher.InvokeAsync(() =>
			renderer.RenderEndpointComponentAsync(typeof(NestedBoundaryRoot), ParameterView.Empty));
		await probe.SelectedInitializationEntered.Task;
		renderTask.IsCompleted.Should().BeFalse();
		probe.ReleaseSelectedInitialization();
		var rendered = await renderTask;
		var selectedComponentId = topology.ResolveSelectedComponentId();

		var rootMarkup = await WriteHtmlAsync(renderer, rendered);
		var selectedMarkup = await renderer.WriteSelectedComponentHtmlAsync(selectedComponentId);

		rootMarkup.Should().Be("<main data-root=\"\"><div><section data-selected=\"\">selected:ready</section></div><aside data-root-sibling=\"\">sibling</aside></main>");
		selectedMarkup.Should().Be("<section data-selected=\"\">selected:ready</section>");
		renderer.SelectedComponentWriterOwner.Should().Be(typeof(StaticHtmlRenderer),
			"selected output must bind to the inherited framework writer, not a Htmxor copy or shadow");
	}

	private static async Task<string> WriteHtmlAsync(StaticHtmlRenderer renderer, HtmlRootComponent content)
	{
		using var output = new StringWriter();
		await renderer.Dispatcher.InvokeAsync(() => content.WriteHtmlTo(output));
		return output.ToString();
	}

	private sealed class ProductionProbeRenderer : HtmxorEndpointCandidateRenderer
	{
		private readonly CompletedTopologyProbe topology;

		public ProductionProbeRenderer(IServiceProvider services, CompletedTopologyProbe topology)
			: base(services, NullLoggerFactory.Instance)
		{
			this.topology = topology;
		}

		protected override ComponentState CreateComponentState(
			int componentId,
			IComponent component,
			ComponentState? parentComponentState)
		{
			var state = base.CreateComponentState(componentId, component, parentComponentState);
			topology.Record(state);
			return state;
		}

		public Type? SelectedComponentWriterOwner
		{
			get
			{
				Action<int, TextWriter> writeComponent = WriteComponentHtml;
				return writeComponent.Method.DeclaringType;
			}
		}

		public async Task<string> WriteSelectedComponentHtmlAsync(int componentId)
		{
			using var output = new StringWriter();
			await Dispatcher.InvokeAsync(() => WriteCompletedComponentHtml(componentId, output));
			return output.ToString();
		}
	}

	private sealed class ProbeRenderer : StaticHtmlRenderer
	{
		private int rootComponentId = -1;
		private readonly CompletedTopologyProbe topology;
		public ProbeRenderer(IServiceProvider services, CompletedTopologyProbe topology) : base(services, NullLoggerFactory.Instance)
		{
			this.topology = topology;
		}
		protected override ComponentState CreateComponentState(int componentId, IComponent component, ComponentState? parentComponentState)
		{
			if (component is NestedBoundaryRoot) rootComponentId = componentId;
			var state = base.CreateComponentState(componentId, component, parentComponentState);
			topology.Record(state);
			return state;
		}
		public string WriteRootComponentHtml() => WriteComponentHtml(rootComponentId);
		public string WriteSelectedComponentHtml(int componentId) => WriteComponentHtml(componentId);
		private string WriteComponentHtml(int componentId)
		{
			componentId.Should().NotBe(-1);
			using var writer = new StringWriter();
			WriteComponentHtml(componentId, writer);
			return writer.ToString();
		}
	}

	private sealed class CompletedTopologyProbe
	{
		private readonly List<ComponentState> componentStates = [];

		public void Record(ComponentState state) => componentStates.Add(state);

		public int ResolveSelectedComponentId()
		{
			var selected = componentStates.Single(state => state.Component is SelectedBoundary);
			selected.ParentComponentState.Should().NotBeNull();
			selected.ParentComponentState!.Component.Should().BeOfType<NestedBoundaryContainer>();
			selected.ParentComponentState.ParentComponentState.Should().NotBeNull();
			selected.ParentComponentState.ParentComponentState!.Component.Should().BeOfType<NestedBoundaryRoot>();
			return selected.ComponentId;
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
