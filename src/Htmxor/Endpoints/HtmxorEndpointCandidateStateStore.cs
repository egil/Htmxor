// Htmxor upstream dependency: src/Shared/Components/PrerenderComponentApplicationStore.cs | reimplements
// Htmxor upstream dependency: src/Shared/Components/ProtectedPrerenderComponentApplicationStore.cs | reimplements
// Htmxor upstream dependency: src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.PrerenderingState.cs | reimplements
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.DataProtection;
using static Microsoft.AspNetCore.Components.Web.RenderMode;

namespace Htmxor.Endpoints;

internal class HtmxorEndpointCandidateStateStore : IPersistentComponentStateStore
{
	private bool persisted;

	public string? PersistedState { get; private set; }

	public Task<IDictionary<string, byte[]>> GetPersistedStateAsync()
		=> Task.FromResult<IDictionary<string, byte[]>>(new Dictionary<string, byte[]>());

	public Task PersistStateAsync(IReadOnlyDictionary<string, byte[]> state)
	{
		if (persisted)
		{
			throw new InvalidOperationException("State already persisted.");
		}

		persisted = true;
		if (state.Count > 0)
		{
			PersistedState = Convert.ToBase64String(SerializeState(state));
		}

		return Task.CompletedTask;
	}

	public virtual bool SupportsRenderMode(IComponentRenderMode renderMode)
		=> renderMode is null or Microsoft.AspNetCore.Components.Web.InteractiveWebAssemblyRenderMode or Microsoft.AspNetCore.Components.Web.InteractiveAutoRenderMode;

	protected virtual byte[] SerializeState(IReadOnlyDictionary<string, byte[]> state)
		=> JsonSerializer.SerializeToUtf8Bytes(state);
}

internal sealed class HtmxorEndpointCandidateCompositeStateStore : IPersistentComponentStateStore, IEnumerable<IPersistentComponentStateStore>
{
	public HtmxorEndpointCandidateCopyOnlyStateStore<Microsoft.AspNetCore.Components.Web.InteractiveServerRenderMode> Server { get; } = new();
	public HtmxorEndpointCandidateCopyOnlyStateStore<Microsoft.AspNetCore.Components.Web.InteractiveAutoRenderMode> Auto { get; } = new();
	public HtmxorEndpointCandidateCopyOnlyStateStore<Microsoft.AspNetCore.Components.Web.InteractiveWebAssemblyRenderMode> WebAssembly { get; } = new();

	public Task<IDictionary<string, byte[]>> GetPersistedStateAsync() => throw new NotSupportedException();

	public Task PersistStateAsync(IReadOnlyDictionary<string, byte[]> state) => Task.CompletedTask;

	public IEnumerator<IPersistentComponentStateStore> GetEnumerator()
	{
		yield return Server;
		yield return Auto;
		yield return WebAssembly;
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal sealed class HtmxorEndpointCandidateCopyOnlyStateStore<TRenderMode> : IPersistentComponentStateStore
	where TRenderMode : IComponentRenderMode
{
	public Dictionary<string, byte[]> Saved { get; private set; } = new();

	public Task<IDictionary<string, byte[]>> GetPersistedStateAsync() => throw new NotSupportedException();

	public Task PersistStateAsync(IReadOnlyDictionary<string, byte[]> state)
	{
		Saved = new Dictionary<string, byte[]>(state);
		return Task.CompletedTask;
	}

	public bool SupportsRenderMode(IComponentRenderMode renderMode) => renderMode is TRenderMode;
}

internal sealed class HtmxorEndpointCandidateProtectedStateStore(IDataProtectionProvider protection)
	: HtmxorEndpointCandidateStateStore
{
	private readonly IDataProtector protector = protection.CreateProtector("Microsoft.AspNetCore.Components.Server.State");

	public override bool SupportsRenderMode(IComponentRenderMode renderMode)
		=> renderMode is null or Microsoft.AspNetCore.Components.Web.InteractiveServerRenderMode or Microsoft.AspNetCore.Components.Web.InteractiveAutoRenderMode;

	protected override byte[] SerializeState(IReadOnlyDictionary<string, byte[]> state)
		=> protector.Protect(base.SerializeState(state));
}
