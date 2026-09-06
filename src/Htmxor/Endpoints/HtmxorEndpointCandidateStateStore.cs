// Htmxor upstream dependency: src/Shared/Components/PrerenderComponentApplicationStore.cs | reimplements
// Htmxor upstream dependency: src/Shared/Components/ProtectedPrerenderComponentApplicationStore.cs | reimplements
// Htmxor upstream dependency: src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.PrerenderingState.cs | reimplements
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.DataProtection;

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
		=> renderMode is null or InteractiveWebAssemblyRenderMode or InteractiveAutoRenderMode;

	protected virtual byte[] SerializeState(IReadOnlyDictionary<string, byte[]> state)
		=> JsonSerializer.SerializeToUtf8Bytes(state);
}

internal sealed class HtmxorEndpointCandidateProtectedStateStore(IDataProtectionProvider protection)
	: HtmxorEndpointCandidateStateStore
{
	private readonly IDataProtector protector = protection.CreateProtector("Microsoft.AspNetCore.Components.Server.State");

	public override bool SupportsRenderMode(IComponentRenderMode renderMode)
		=> renderMode is null or InteractiveServerRenderMode or InteractiveAutoRenderMode;

	protected override byte[] SerializeState(IReadOnlyDictionary<string, byte[]> state)
		=> protector.Protect(base.SerializeState(state));
}
