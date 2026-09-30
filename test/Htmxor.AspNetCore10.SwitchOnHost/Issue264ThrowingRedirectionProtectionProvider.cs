using Microsoft.AspNetCore.DataProtection;

namespace Htmxor.AspNetCore10.SwitchOnHost;

// Wraps the host's real ephemeral data-protection provider so that only the opaque-redirection purpose throws;
// antiforgery, TempData, and persisted-state protection continue working normally through the inner provider.
// Used only by the "--throw-on-opaque-redirection-protect" host variant that PR #265 review
// discussion_r4140656921 probes: if OpaqueRedirection.CreateProtectedRedirectionUrl fails mid-way through
// building the after-start redirection template, does a partial <blazor-ssr> fragment reach the wire?
internal sealed class Issue264ThrowingRedirectionProtectionProvider(IDataProtectionProvider inner) : IDataProtectionProvider
{
	// Must match OpaqueRedirection's own RedirectionDataProtectionProviderPurpose exactly (src/Htmxor/Rendering/OpaqueRedirection.cs).
	public const string RedirectionPurpose = "Microsoft.AspNetCore.Components.Endpoints.OpaqueRedirection,V1";

	public IDataProtector CreateProtector(string purpose) =>
		purpose == RedirectionPurpose ? new ThrowingProtector() : inner.CreateProtector(purpose);

	private sealed class ThrowingProtector : IDataProtector
	{
		public IDataProtector CreateProtector(string purpose) => this;

		public byte[] Protect(byte[] plaintext) => throw new InvalidOperationException(
			"Issue264ThrowingRedirectionProtectionProvider: simulated opaque-redirection protection failure.");

		public byte[] Unprotect(byte[] protectedData) => throw new InvalidOperationException(
			"Issue264ThrowingRedirectionProtectionProvider: simulated opaque-redirection protection failure.");
	}
}
