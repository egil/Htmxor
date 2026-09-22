using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

// Issue #238: GitHubIssueUpserter.UpsertAsync matches an existing tracker issue by a dedupe
// identity carried on an `Identity: <value>` body line and updates a match with a full body
// replace. #232 answered the hazard that raised the issue — a drift finding and an unresolved-
// watch finding now derive separate identities in MonitorReports — but left that decision
// unpinned by a test: nothing drove UpsertAsync twice with different finding kinds sharing one
// listing to show a later run cannot erase an earlier one's body. These tests close that gap.
public sealed class IssueIdentityTests
{
	// The exact wrong path #219 shipped for two releases before it was noticed: see issue #232.
	// Reused here only to produce a genuine unresolved-watch finding; ManifestDependencyPolicy
	// coverage of it is unrelated to this file.
	private const string WrongFilePath = "src/Components/Endpoints/src/CacheView/CacheViewTextWriter.cs";

	// Criterion 3: the identity scheme's intent — a drift finding and an unresolved-watch finding
	// for the same supported framework never share an identity — is asserted directly against
	// MonitorReports.Create, the real production function that derives both identities, for a
	// real MonitorRequest. Parameterized over two different SupportedMajorVersion values (net10
	// and net11 both ship watches, see WatchFrameworkScopeTests) because both identity format
	// strings interpolate that version: a scheme that distinguished only by version, and not by
	// finding kind, would still read as "the two identities differ" if that were only checked
	// against one hardcoded version by coincidence of the literal string chosen for the test;
	// checking it holds for both configured frameworks makes the claim about the kind, not about
	// one version's literal text.
	[Theory]
	[InlineData(10)]
	[InlineData(11)]
	public void Drift_and_unresolved_identities_are_distinct_for_the_same_supported_major_version(int majorVersion)
	{
		var framework = majorVersion == 10 ? Fixture.Net10Framework() : Fixture.Net11Framework();

		var drift = DriftOnlyResult(framework);
		var unresolved = UnresolvedOnlyResult(framework);

		var driftIdentity = Assert.Single(drift.Issues).Identity;
		var unresolvedIdentity = Assert.Single(unresolved.Issues).Identity;
		Assert.NotEqual(driftIdentity, unresolvedIdentity);
	}

	// Criteria 1 and 2: two sequential UpsertAsync calls, one carrying a drift finding and the
	// other carrying an unresolved-watch finding, must leave both findings discoverable in the
	// tracker afterwards, in both orderings. RecordingIssueTransport persists what each call
	// actually wrote, so the second call's own listing request is answered from what the first
	// call really left, not from a hand-stubbed guess at it (see RecordingIssueTransport's own
	// remarks). Both GitHubIssueUpserter and the identity derivation (MonitorReports.Create) are
	// real; only the HTTP transport is fake.
	//
	// If the two kinds shared one identity, the second call's listing would match the issue the
	// first call created, and UpdateAsync's full body replace would overwrite it: the second
	// write's action would read Updated instead of Created, the tracker would still hold exactly
	// one issue after both calls, and its body would carry only the second finding. Every
	// assertion below is written to catch exactly that shape; see the receipt for the recorded
	// inversion that confirms it.
	[Theory]
	[InlineData("drift-then-unresolved")]
	[InlineData("unresolved-then-drift")]
	public async Task Two_upserts_carrying_divergent_findings_leave_both_discoverable_in_the_tracker(string ordering)
	{
		var framework = Fixture.Net10Framework();
		var drift = DriftOnlyResult(framework);
		var unresolved = UnresolvedOnlyResult(framework);
		var (first, second) = ordering == "drift-then-unresolved" ? (drift, unresolved) : (unresolved, drift);

		var transport = new RecordingIssueTransport();
		using var client = new HttpClient(transport, disposeHandler: false) { BaseAddress = new Uri("https://api.github.test") };
		var upserter = new GitHubIssueUpserter(client);

		var firstWrite = await upserter.UpsertAsync(first);
		var secondWrite = await upserter.UpsertAsync(second);

		// Each call created its own issue rather than the second matching and overwriting the
		// first's: this is the assertion that reddens under a collapsed identity.
		Assert.Equal(new IssueWriteResult(IssueWriteAction.Created, 1, null), firstWrite);
		Assert.Equal(new IssueWriteResult(IssueWriteAction.Created, 2, null), secondWrite);

		// Both findings are discoverable afterwards, with the exact body MonitorReports.Create
		// produced for each, not a body one of them overwrote.
		Assert.Equal(2, transport.Issues.Count);
		Assert.Equal(first.Issues[0].Body, transport.Issues[0].Body);
		Assert.Equal(second.Issues[0].Body, transport.Issues[1].Body);

		// The second call actually observed the state the first one left: two listing GETs were
		// made (one per UpsertAsync call), not one shared assumption.
		Assert.Equal(2, transport.Requests.Count(request => request.Method == HttpMethod.Get));
	}

	private static MonitorResult DriftOnlyResult(FrameworkBaseline framework) => MonitorReports.Create(
		Request(framework), MonitorStatus.Drift, Upstream(framework),
		[new SourceChange(ExpectedMonitorArtifacts.Invoker, ChangeKind.Changed, ReviewClassification.ParityRequired)],
		[]);

	private static MonitorResult UnresolvedOnlyResult(FrameworkBaseline framework) => MonitorReports.Create(
		Request(framework), MonitorStatus.UnresolvedWatch, Upstream(framework),
		[], [], unresolvedWatchPaths: [WrongFilePath]);

	private static MonitorRequest Request(FrameworkBaseline framework) =>
		new(Fixture.ManifestFor(framework), framework, framework.ReviewedTag, framework.ReviewedCommit);

	private static UpstreamRevision Upstream(FrameworkBaseline framework) =>
		new(framework.ReviewedTag, framework.ReviewedCommit);
}
