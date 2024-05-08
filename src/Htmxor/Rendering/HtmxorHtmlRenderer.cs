using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace Htmxor.Rendering;

/// <summary>
/// Provides a mechanism for rendering components non-interactively as HTML markup.
/// </summary>
public sealed class HtmxorHtmlRenderer : IDisposable, IAsyncDisposable
{
	private readonly HtmxorRenderer renderer;

	/// <summary>
	/// Constructs an instance of <see cref="HtmxorHtmlRenderer"/>.
	/// </summary>
	/// <param name="services">The services to use when rendering components.</param>
	/// <param name="loggerFactory">The logger factory to use.</param>
	public HtmxorHtmlRenderer(IServiceProvider services, ILoggerFactory loggerFactory)
	{
		renderer = new HtmxorRenderer(services, loggerFactory);
	}

	/// <inheritdoc />
	public void Dispose()
		=> renderer.Dispose();

	/// <inheritdoc />
	public ValueTask DisposeAsync()
		=> renderer.DisposeAsync();

	/// <summary>
	/// Gets the <see cref="Components.Dispatcher" /> associated with this instance. Any calls to
	/// <see cref="RenderComponentAsync{TComponent}()"/> or <see cref="BeginRenderingComponent{TComponent}()"/>
	/// must be performed using this <see cref="Components.Dispatcher" />.
	/// </summary>
	public Dispatcher Dispatcher => renderer.Dispatcher;

	/// <summary>
	/// Adds an instance of the specified component and instructs it to render. The resulting content represents the
	/// initial synchronous rendering output, which may later change. To wait for the component hierarchy to complete
	/// any asynchronous operations such as loading, await <see cref="HtmxorRootComponent.QuiescenceTask"/> before
	/// reading content from the <see cref="HtmxorRootComponent"/>.
	/// </summary>
	/// <typeparam name="TComponent">The component type.</typeparam>
	/// <returns>An <see cref="HtmxorRootComponent"/> instance representing the render output.</returns>
	public HtmxorRootComponent BeginRenderingComponent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>() where TComponent : IComponent
		=> renderer.BeginRenderingComponent(typeof(TComponent), ParameterView.Empty);

	/// <summary>
	/// Adds an instance of the specified component and instructs it to render. The resulting content represents the
	/// initial synchronous rendering output, which may later change. To wait for the component hierarchy to complete
	/// any asynchronous operations such as loading, await <see cref="HtmxorRootComponent.QuiescenceTask"/> before
	/// reading content from the <see cref="HtmxorRootComponent"/>.
	/// </summary>
	/// <typeparam name="TComponent">The component type.</typeparam>
	/// <param name="parameters">Parameters for the component.</param>
	/// <returns>An <see cref="HtmxorRootComponent"/> instance representing the render output.</returns>
	public HtmxorRootComponent BeginRenderingComponent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>(
		ParameterView parameters) where TComponent : IComponent
		=> renderer.BeginRenderingComponent(typeof(TComponent), parameters);

	/// <summary>
	/// Adds an instance of the specified component and instructs it to render. The resulting content represents the
	/// initial synchronous rendering output, which may later change. To wait for the component hierarchy to complete
	/// any asynchronous operations such as loading, await <see cref="HtmxorRootComponent.QuiescenceTask"/> before
	/// reading content from the <see cref="HtmxorRootComponent"/>.
	/// </summary>
	/// <param name="componentType">The component type. This must implement <see cref="IComponent"/>.</param>
	/// <returns>An <see cref="HtmxorRootComponent"/> instance representing the render output.</returns>
	public HtmxorRootComponent BeginRenderingComponent(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType)
		=> renderer.BeginRenderingComponent(componentType, ParameterView.Empty);

	/// <summary>
	/// Adds an instance of the specified component and instructs it to render. The resulting content represents the
	/// initial synchronous rendering output, which may later change. To wait for the component hierarchy to complete
	/// any asynchronous operations such as loading, await <see cref="HtmxorRootComponent.QuiescenceTask"/> before
	/// reading content from the <see cref="HtmxorRootComponent"/>.
	/// </summary>
	/// <param name="componentType">The component type. This must implement <see cref="IComponent"/>.</param>
	/// <param name="parameters">Parameters for the component.</param>
	/// <returns>An <see cref="HtmxorRootComponent"/> instance representing the render output.</returns>
	public HtmxorRootComponent BeginRenderingComponent(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType,
		ParameterView parameters)
		=> renderer.BeginRenderingComponent(componentType, parameters);

	/// <summary>
	/// Adds an instance of the specified component and instructs it to render, waiting
	/// for the component hierarchy to complete asynchronous tasks such as loading.
	/// </summary>
	/// <typeparam name="TComponent">The component type.</typeparam>
	/// <returns>A task that completes with <see cref="HtmxorRootComponent"/> once the component hierarchy has completed any asynchronous tasks such as loading.</returns>
	public Task<HtmxorRootComponent> RenderComponentAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>() where TComponent : IComponent
		=> RenderComponentAsync<TComponent>(ParameterView.Empty);

	/// <summary>
	/// Adds an instance of the specified component and instructs it to render, waiting
	/// for the component hierarchy to complete asynchronous tasks such as loading.
	/// </summary>
	/// <param name="componentType">The component type. This must implement <see cref="IComponent"/>.</param>
	/// <returns>A task that completes with <see cref="HtmxorRootComponent"/> once the component hierarchy has completed any asynchronous tasks such as loading.</returns>
	public Task<HtmxorRootComponent> RenderComponentAsync(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType)
		=> RenderComponentAsync(componentType, ParameterView.Empty);

	/// <summary>
	/// Adds an instance of the specified component and instructs it to render, waiting
	/// for the component hierarchy to complete asynchronous tasks such as loading.
	/// </summary>
	/// <typeparam name="TComponent">The component type.</typeparam>
	/// <param name="parameters">Parameters for the component.</param>
	/// <returns>A task that completes with <see cref="HtmxorRootComponent"/> once the component hierarchy has completed any asynchronous tasks such as loading.</returns>
	public Task<HtmxorRootComponent> RenderComponentAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>(
		ParameterView parameters) where TComponent : IComponent
		=> RenderComponentAsync(typeof(TComponent), parameters);

	/// <summary>
	/// Adds an instance of the specified component and instructs it to render, waiting
	/// for the component hierarchy to complete asynchronous tasks such as loading.
	/// </summary>
	/// <param name="componentType">The component type. This must implement <see cref="IComponent"/>.</param>
	/// <param name="parameters">Parameters for the component.</param>
	/// <returns>A task that completes with <see cref="HtmxorRootComponent"/> once the component hierarchy has completed any asynchronous tasks such as loading.</returns>
	public async Task<HtmxorRootComponent> RenderComponentAsync(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType,
		ParameterView parameters)
	{
		var content = BeginRenderingComponent(componentType, parameters);
		await content.QuiescenceTask;
		return content;
	}
}
