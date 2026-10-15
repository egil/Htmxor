using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Htmxor.Generators;

internal enum HtmxorDiagnosticCause
{
	RouteTargetOrOrigin = 4,
	MalformedRouteMember = 5,
	InferredLambda = 6,
	InferredEventCallbackField = 7,
	InferredEventCallbackParameter = 8,
	InferredMethodCall = 9,
	InferredComputedExpression = 10,
	InferredOverload = 11,
	InferredGeneric = 12,
	StaticHandler = 13,
	UnroutedBinding = 14,
	ImplicitGetBinding = 15,
	ValueTaskReturn = 16,
	ExplicitOverload = 17,
	AsyncVoid = 18,
	TaskOfTReturn = 19,
	EventArgsParameter = 20,
	ObjectParameter = 21,
	ExplicitGeneric = 22,
	RefOrOutParameter = 23,
	OptionalParameter = 24,
	OtherUnsupportedParameter = 25,
	OtherValueReturn = 26,
	UnnamedEditForm = 27,
	UnnamedPostForm = 28,
	InvalidExplicitVerb = 29,
	ExplicitUnroutedOwner = 30,
	ConflictingHandlers = 31,
	NamedFormPostConflict = 32,
	ExplicitMethodsConflict = 33,
	NormalOnlyAction = 34,
	NonordinaryExplicitAction = 35,
	MisplacedBinding = 36,
	MismatchedRazorOwner = 37,
	LocalPageRouteMismatch = 38,
	StockRouteWithoutLocalPage = 39,
	NonmemberHandler = 40,
	NonmethodHandler = 41,
	UnmatchedCompiledBinding = 42,
	UnmatchedCompiledForm = 43,
}

internal readonly struct HtmxorDiagnosticIssue
{
	public HtmxorDiagnosticIssue(HtmxorDiagnosticCause cause, string reason)
	{
		Cause = cause;
		Reason = reason;
	}

	public HtmxorDiagnosticCause Cause { get; }

	public string Reason { get; }

	public Diagnostic ToDiagnostic(Location location)
		=> Diagnostic.Create(HtmxorDiagnosticRules.Get(Cause), location, Reason);

	public HtmxorDiagnosticIssue WithReason(string reason) => new(Cause, reason);
}

internal static class HtmxorDiagnosticRules
{
	private static readonly DiagnosticDescriptor Rule004 = new(
		"HTMXOR004", "Unsupported HTMX-only route target or origin", "HTMX-only route is not available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule005 = new(
		"HTMXOR005", "Malformed HTMX-only route member or method", "HTMX-only route is not available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule006 = new(
		"HTMXOR006", "Inferred action uses a lambda or closure", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule007 = new(
		"HTMXOR007", "Inferred action uses an EventCallback field", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule008 = new(
		"HTMXOR008", "Inferred action uses an EventCallback parameter", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule009 = new(
		"HTMXOR009", "Inferred action uses a method call", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule010 = new(
		"HTMXOR010", "Inferred action uses a computed expression", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule011 = new(
		"HTMXOR011", "Inferred action uses an overloaded handler", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule012 = new(
		"HTMXOR012", "Inferred action uses a generic handler", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule013 = new(
		"HTMXOR013", "Action handler is static", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule014 = new(
		"HTMXOR014", "Inferred action has no route owner", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule015 = new(
		"HTMXOR015", "GET action binding is redundant", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule016 = new(
		"HTMXOR016", "Action handler returns ValueTask", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule017 = new(
		"HTMXOR017", "Explicit action handler is overloaded", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule018 = new(
		"HTMXOR018", "Action handler is async void", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule019 = new(
		"HTMXOR019", "Action handler returns Task of a value", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule020 = new(
		"HTMXOR020", "Action handler accepts EventArgs", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule021 = new(
		"HTMXOR021", "Action handler accepts object", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule022 = new(
		"HTMXOR022", "Explicit action handler is generic", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule023 = new(
		"HTMXOR023", "Action handler has a ref or out parameter", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule024 = new(
		"HTMXOR024", "Action handler has an optional parameter", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule025 = new(
		"HTMXOR025", "Action handler has unsupported parameters", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule026 = new(
		"HTMXOR026", "Action handler returns an unsupported value", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule027 = new(
		"HTMXOR027", "EditForm has no name", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule028 = new(
		"HTMXOR028", "POST form has no name", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule029 = new(
		"HTMXOR029", "Explicit action uses an unsupported method", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule030 = new(
		"HTMXOR030", "Explicit action has no local route owner", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule031 = new(
		"HTMXOR031", "Route method has conflicting handlers", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule032 = new(
		"HTMXOR032", "Named form conflicts with POST action", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule033 = new(
		"HTMXOR033", "Action or form is outside explicit route methods", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule034 = new(
		"HTMXOR034", "Action is on a normal-only component", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule035 = new(
		"HTMXOR035", "Explicit action is not an ordinary method", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule036 = new(
		"HTMXOR036", "Inferred binding is outside owner markup", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule037 = new(
		"HTMXOR037", "Inferred binding has no matching Razor owner", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule038 = new(
		"HTMXOR038", "Local page binding conflicts with compiled route", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule039 = new(
		"HTMXOR039", "Stock route binding has no local page", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule040 = new(
		"HTMXOR040", "Action handler is not a component member", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule041 = new(
		"HTMXOR041", "Action handler is not a method", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule042 = new(
		"HTMXOR042", "Compiled binding has no matching action", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly DiagnosticDescriptor Rule043 = new(
		"HTMXOR043", "Compiled form has no matching POST inference", "no usable action is available: {0}",
		"Htmxor.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true,
		customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

	private static readonly ImmutableArray<DiagnosticDescriptor> Rules = ImmutableArray.Create(
		Rule004,
		Rule005,
		Rule006,
		Rule007,
		Rule008,
		Rule009,
		Rule010,
		Rule011,
		Rule012,
		Rule013,
		Rule014,
		Rule015,
		Rule016,
		Rule017,
		Rule018,
		Rule019,
		Rule020,
		Rule021,
		Rule022,
		Rule023,
		Rule024,
		Rule025,
		Rule026,
		Rule027,
		Rule028,
		Rule029,
		Rule030,
		Rule031,
		Rule032,
		Rule033,
		Rule034,
		Rule035,
		Rule036,
		Rule037,
		Rule038,
		Rule039,
		Rule040,
		Rule041,
		Rule042,
		Rule043);
	public static ImmutableArray<DiagnosticDescriptor> RouteDiagnostics { get; } =
		Rules.Take(2).ToImmutableArray();

	public static ImmutableArray<DiagnosticDescriptor> ActionDiagnostics { get; } =
		Rules.Skip(2).ToImmutableArray();

	public static DiagnosticDescriptor Get(HtmxorDiagnosticCause cause)
		=> Rules[(int)cause - 4];
}
