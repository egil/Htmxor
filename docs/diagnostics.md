# Htmxor diagnostic reference

This page covers the 41 active diagnostics, HTMXOR003 through HTMXOR043, in the
unreleased v1 source. HTMXOR001 and HTMXOR002 are retired. Each ID below is a
stable help-link anchor, including when several descriptor instances or distinct
causes share that ID. Read the reason in the diagnostic to choose the relevant
fix; correcting one cause does not necessarily correct the other causes on the
same declaration.

These diagnostics are enabled errors and cannot be disabled through diagnostic
configuration. They describe component-owned routes and actions; adding an
application controller or endpoint is not their remediation. `hx-*` attributes
never grant server methods. GET is implicit. Explicit actions support POST, PUT,
PATCH, DELETE and QUERY on the request-owned component instance.

## Reading the diagnostics

Local route, explicit-action, and recoverable callback/form facts can be reported
without complete-compilation analysis. Source scanning also reports binding
expression, placement and per-file handler conflicts. Manifest completeness and
cross-source conflicts require complete analysis. Some IDs have both local and
aggregate descriptors; an ID alone does not specify when its cause is available.
An external form location without a source tree remains complete-analysis work.
This describes analyzer boundaries, not a guarantee about a particular editor UI.

C# and Razor retain ownership of language and binding errors. In particular, an
inferred handler whose Razor callback conversion fails does not acquire an
additional Htmxor handler-shape error. The shared shape checks below also apply
to explicit actions, where those shapes can be diagnosed. Fix compiler errors
first without assuming unrelated valid declarations are suppressed. A missing
generated action explained by a known fault on that binding is not a second,
independent completeness error.

Stock ASP0017 owns route-template grammar; ASP.NET Core resolves constraints at
startup. Htmxor checks its own route declaration and leading-slash requirements.
Authorization attributes and components in arbitrary project folders are not
rejected merely for those choices. Runtime catalogs share reason text only for
rules they actually enforce; source provenance and expression checks remain
compiler concerns, and runtime manifest/metadata defenses do not become new
compiler restrictions.

Supported callback signatures are `void M()`, `void M(HtmxEventArgs args)`,
`Task M()` and `Task M(HtmxEventArgs args)`: ordinary, non-generic instance
methods, with a required by-value argument when present. An explicit
`[HtmxAction("PUT")]` on such a method is the narrow fallback for a binding that
cannot be inferred. Remove or replace the unsupported binding as well; the
attribute does not make an invalid expression or signature supported.

## HTMXOR003

**Unsupported normal-only route declaration.** `DisableHtmxDirectRouting` is
supplied from `_Imports.razor`, is combined with `HtmxRoute` on the same type, or
is placed on an abstract/non-component type or one without its own stock
`@page`/`Route` declaration. Several of these causes can be reported together.

Move the marker onto the concrete stock-route component's own declaration.
Choose normal-only routing or an HTMX-only route, and remove the incompatible
marker or `HtmxRoute`. An inherited route alone does not satisfy the local-route
requirement.

## HTMXOR004

**Unsupported HTMX-only route target or origin.** Causes include:

- A nested target, or a target that is not a concrete, non-generic Blazor component.
- An `HtmxRoute` inherited through `_Imports.razor` rather than authored on its owner.
- A C# route attribute on a Razor component outside its matching `.razor.cs` partial.
- A generated Razor declaration that does not match the component's file/folder
  and `@namespace` identity in the Razor manifest.
- A stock `Route` on the component or its base chain, or an unresolved Blazor
  `RouteAttribute` symbol.

Use a top-level concrete component. Put `HtmxRoute` in its `.razor` file or
matching `.razor.cs` partial, or on an all-C# component. Correct the component
namespace and source/build inputs if the manifest does not match. Choose a stock
route or an HTMX-only route rather than combining them; repair framework
references if `RouteAttribute` cannot resolve. There is no project-root-folder
restriction. Manifest matching is an aggregate check; the other supported local
origin/target facts are reported locally.

## HTMXOR005

**Malformed HTMX-only route member or method.** Each bound member is checked
independently:

- A C# `HtmxRoute` declaration omits `Methods`. Supply it explicitly, including on
  a matching Razor code-behind partial; omission-based inference belongs to Razor.
- The template is null/blank or lacks `/` at its start. Supply one nonblank
  constant template beginning with `/`.
- Explicit `Methods` is null, empty, contains null/unsupported values, or repeats
  a method ignoring case. Use a nonempty unique subset of GET, POST, PUT, PATCH,
  DELETE and QUERY.
- `CurrentUrl` is not a valid relative or absolute HTTP(S) URI. Use a supported
  URI, null or whitespace.
- `Target` is not a valid single element identity. Supply one tag identity or
  `tag#id` identity without whitespace/control characters, null or whitespace. `Targets` must be a constant array of valid element identities
  (or null); remove invalid or null elements.
- A bound named argument is unsupported. Remove that argument and use the
  supported route representation members.

Malformed language/attribute binding stays compiler-owned; an error in one
argument does not hide independent errors in other bound arguments. Route grammar
and constraint resolution retain their framework owners.

## HTMXOR006

**Inferred action uses a lambda or closure.** The binding contains a lambda or
anonymous method rather than a handler method name. Move the operation into a
supported component instance method and declare the action explicitly, replacing
the unsupported binding. Scanned expressions and recovered compiled-only
expressions are alternative reporting paths for this cause.

## HTMXOR007

**Inferred action uses an EventCallback field.** A resolved binding names an
`EventCallback` or `EventCallback<T>` field rather than a method. Use a supported
component instance method and an explicit `HtmxAction` declaration.

## HTMXOR008

**Inferred action uses an EventCallback parameter.** The resolved value is an
EventCallback method parameter, or an EventCallback property marked as a
component parameter or cascading parameter. It is not the request owner's
handler method. Declare a supported instance method on that owner explicitly.
An ordinary EventCallback property without those parameter markers is
[HTMXOR041](#htmxor041).

## HTMXOR009

**Inferred action uses a method call.** A binding such as `GetHandler()` is an
invocation rather than a handler name. Replace it with a supported component
instance method and declare `HtmxAction` explicitly. This applies to scanned
expressions and recovered compiled-only invocations.

## HTMXOR010

**Inferred action uses a computed expression.** The nonempty binding cannot be
read as a bare method name or `this.Method`, and is neither a lambda nor an
invocation. Use a supported instance method and an explicit action declaration.
Parenthesized method names and the supported Razor `@`/quoted spellings do not
become computed expressions merely because of that spelling. Missing or empty
values that Razor drops are not action declarations. C#/Razor still owns malformed
language; this diagnostic is not a replacement language parser.

## HTMXOR011

**Inferred action uses an overloaded handler.** A successfully bound component
method has more than one member in its method group. Give the intended instance
handler a unique name and declare `HtmxAction`. If overload resolution itself
fails, that binding error belongs to the compiler.

## HTMXOR012

**Inferred action uses a generic handler.** The resolved method is generic,
including a method for which type arguments were successfully inferred. Make the
handler non-generic and declare `HtmxAction`. Failure to infer type arguments
remains a compiler binding error.

## HTMXOR013

**Action handler is static.** A component/base-chain inferred handler or an
explicit action method is static. Make it an instance method; for an inferred
binding, use the explicit declaration advised by the diagnostic. Imported
external static methods are instead a nonmember cause, [HTMXOR040](#htmxor040).
A compiler-invalid static access does not establish a valid inferred callback.

## HTMXOR014

**Inferred action has no route owner.** The component has neither its own stock
route nor an `HtmxRoute`. Add `@page` or `HtmxRoute` to the owner, or remove the
binding. This owner fact can be reported even when the handler cannot bind; it
prevents dependent route-shape/allow-list guesses, not independent handler or
normal-only diagnostics.

## HTMXOR015

**GET action binding is redundant.** A nonempty `@onget` binding declares nothing:
GET is already implicit. Remove it. `HtmxAction` only declares the supported
non-GET methods, so replacing it with `[HtmxAction("GET")]` is not a fix. Scanned
and compiled-only bindings can report this cause; a separate expression or
placement fault can still be independent.

## HTMXOR016

**Action handler returns ValueTask.** The handler returns `ValueTask` or
`ValueTask<T>`. Use `void` or `Task` without a result. Explicit actions can expose
this shared shape cause. When an inferred Razor callback rejects that conversion,
its compiler error owns the failure instead.

## HTMXOR017

**Explicit action handler is overloaded.** The declaring type contains multiple
methods with the explicit handler's name. Give the action handler a unique name.
The inferred method-group check has its own ID, [HTMXOR011](#htmxor011).

## HTMXOR018

**Action handler is async void.** Return `Task` instead of `async void` so the
request can await completion. Other independent shape or owner faults still need
their own fixes.

## HTMXOR019

**Action handler returns Task of a value.** Replace `Task<T>` with `Task` and
update component state rather than returning a result from the callback.

## HTMXOR020

**Action handler accepts EventArgs.** The parameter type is `System.EventArgs`
rather than `HtmxEventArgs`. Change it to a required by-value `HtmxEventArgs`, or
remove the parameter if unused. The check applies per parameter.

## HTMXOR021

**Action handler accepts object.** Change the `object` parameter to a required
by-value `HtmxEventArgs`, or remove it if unused. A callback conversion that C#
accepts is not by itself a supported Htmxor action signature.

## HTMXOR022

**Explicit action handler is generic.** Make the method carrying `HtmxAction`
non-generic. The inferred generic-handler case is [HTMXOR012](#htmxor012).

## HTMXOR023

**Action handler has a by-reference parameter.** This includes `ref`, `out` and
`in`, despite the descriptor title mentioning ref/out. Use a by-value
`HtmxEventArgs` parameter. Inferred callbacks rejected by Razor's conversion
remain compiler-owned; explicit actions exercise this shape check directly.

## HTMXOR024

**Action handler has an optional parameter.** Make the parameter required.
If its type is also unsupported, correcting optionality alone is insufficient.

## HTMXOR025

**Action handler has unsupported parameters.** Two distinct causes share this ID:

- More than one parameter: use zero parameters or one required `HtmxEventArgs`.
- A parameter has another unsupported type, other than the separately classified
  `EventArgs` and `object`: use `HtmxEventArgs` or remove the unused parameter.

Each incorrect parameter can report a cause in addition to the arity cause;
reports are not collapsed just because the ID is identical. `dynamic` can bind
as an inferred callback but still violates the required type. Other inferred
conversions, including excess arity, may fail in the compiler before Htmxor's
shape checks; that does not create an additional inferred Htmxor error.

## HTMXOR026

**Action handler returns another unsupported value.** The return type is neither
`void` nor exactly `Task`, and is not a separately classified ValueTask/Task<T>.
Use `void` or `Task`. Compiler-rejected inferred callback conversions remain
compiler-owned; explicit actions can report this cause.

## HTMXOR027

**EditForm has no name.** A valid POST `EditForm` on an `HtmxRoute` owner has no
name. Supply its stock Blazor `FormName`. This rule does not impose a new naming
restriction on stock-only routes. Invalid name expressions are not guessed to be
unnamed; independent method restrictions can still be reported.

## HTMXOR028

**POST form has no name.** A valid plain POST form on an `HtmxRoute` owner has no
name. Add the stock Razor `@formname` directive attribute. Non-POST and stock-only
forms do not acquire this Htmxor naming rule. As with EditForm, an invalid name
is not treated as a known missing name.

## HTMXOR029

**Explicit action uses an unsupported method.** `HtmxAction` must specify POST,
PUT, PATCH, DELETE or QUERY (case-insensitively). Replace the invalid/null method
with one of those supported methods or remove the action. GET is implicit and
is not an explicit action method. Compiler-invalid attribute expressions retain
their compiler diagnostics.

## HTMXOR030

**Explicit action has no local route owner.** The method's declaring type is not
a Blazor component with its own `Route`/`HtmxRoute`; an inherited route alone is
insufficient. Put `HtmxAction` on a supported method of the component that owns
the route, or supply that component's own route. This is not permission to move
the callback into a separate endpoint or static handler.

## HTMXOR031

**Route method has conflicting handlers.** Multiple inferred bindings for one
method on a route owner name different handlers, or explicit declarations
conflict with another explicit/inferred handler for that method. Select the same
handler for that method on the owner. Repeated declarations of the same handler
are not a different-handler conflict. Inferred per-file conflicts come from the
source scan; cross-source explicit conflicts remain aggregate analysis.

## HTMXOR032

**Named form conflicts with POST action.** A valid named form shares its owner
with an inferred POST binding, or with an explicit POST `HtmxAction`. Choose the
stock named-form path or the POST action on that owner and remove the competing
one. The explicit-action reason specifically identifies `HtmxAction`; the form
reason identifies the POST action. These are distinct reports, not duplicate
wording that can be collapsed by ID.

A source-backed form with a same-tree POST binding can report locally;
cross-tree form conflicts and the explicit-action conflict require complete
analysis. Invalid form names do not establish this conflict. A normal stock
route can also have this conflict; it is not limited to `HtmxRoute`.

## HTMXOR033

**Action or form is outside explicit route methods.** An explicit
`HtmxRoute.Methods` list is authoritative. Three reasons share this ID:

- An inferred binding's method is not allowed: add that method or remove the binding.
- An explicit `HtmxAction` method is not allowed: add it or remove the action.
- A valid POST form is excluded: add POST or remove the form.

The form check requires a supported explicit Methods set; malformed sets have
[HTMXOR005](#htmxor005) instead of a speculative form allow-list consequence.
The callback/action checks can establish absence from a bound non-error Methods
value independently of other list faults. A known form method exclusion does
not require a valid form name, so it can accompany a name diagnostic. Do not
assume that the same ID means identical prerequisites for all three cases.

## HTMXOR034

**Action is on a normal-only component.** An inferred binding or explicit
`HtmxAction` belongs to a component marked `DisableHtmxDirectRouting`. Remove the
binding/action if the component should remain normal-only, or remove the marker
if it should expose that action. These reasons remain distinct from
[HTMXOR003](#htmxor003), which validates the marker's placement and route conflict.

## HTMXOR035

**Explicit action is not an ordinary method.** The attribute is on a nonordinary
callback, such as an accessor, operator/conversion, destructor, local function or
lambda. Move it to an ordinary component instance method with a supported
signature. Compiler-invalid declarations remain compiler-owned. Reflection has
different representations for some such members; do not infer a new runtime
acceptance rule from this compiler classification.

## HTMXOR036

**Inferred binding is outside owner markup.** A binding occurs in a Razor
template or `@code` markup. Move it into the component's own markup, or replace
it with a supported explicit action. Ordinary owner markup nested inside control
flow is not rejected simply for being nested.

## HTMXOR037

**Inferred binding has no matching Razor owner.** The scanned declaration does
not match the compiled Razor component and its generated path. Correct the
file/folder, `@namespace`, partial-type identity and build inputs so they describe
the same owner. This aggregate ownership failure must be corrected before
assuming the binding's dependent generated action should exist.

## HTMXOR038

**Local page binding conflicts with compiled route.** A component with a local
`@page` action must compile with exactly one stock route and no `HtmxRoute`.
Remove conflicting route declarations or choose the intended route model. When
there is no route at all, [HTMXOR014](#htmxor014) is the prerequisite failure.
A compiled binding without a correlated source declaration is not evidence of a
local `@page`.

## HTMXOR039

**Stock route binding has no local page.** A correlated action declaration lacks
a local `@page`, but its component has a compiled stock route. Put the stock
`@page` on the action-owning Razor component, or use the intended HTMX-only route
without a conflicting stock route. Merely finding a compiled binding with no
source declaration does not establish this cause.

## HTMXOR040

**Action handler is not a component member.** The resolved handler is a local
function, a local/parameter/range variable, or a method outside the component's
base chain, including an imported static or extension method. Use an instance
method on the request-owned component and declare a supported explicit action
when inference is unavailable. Htmxor does not then classify an external method's
shape as though it were a component handler. Unresolved or ambiguous symbols
remain compiler-owned.

## HTMXOR041

**Action handler is not a method.** The resolved binding names another nonmethod
value, such as a delegate property/field or an ordinary EventCallback property.
Use `HtmxAction` on a supported component instance method. EventCallback fields
and component parameters have the specific [007](#htmxor007) and
[008](#htmxor008) causes; local/nonmember values use [040](#htmxor040).

## HTMXOR042

**Compiled binding has no matching action.** A valid compiled binding has no
matching generated action for its component, HTTP method and handler, and no
known fault on that binding explains the absence. Keep the declaration in its
owner's supported Razor markup, ensure Razor source/build inputs reach Htmxor's
generator consistently, or replace the unsupported inference path with an
explicit supported action. This is a complete-analysis manifest check. An error
on a different binding does not excuse this mismatch, and a known error on this
binding should be fixed rather than treated as another completeness cause.

## HTMXOR043

**Compiled form has no matching POST inference.** A valid named POST form on an
`HtmxRoute` owner with omitted `Methods` has no matching generated POST inference,
without a POST-action conflict explaining it. Move the form into the route
owner's own Razor markup, or replace the form with a supported explicit POST
action. This is complete-analysis work; unnamed/invalid-name forms, stock-only
routes and routes with an explicit Methods argument do not establish this
omission-based inference claim.

## Maintaining this reference

The descriptor/document contract discovers all public analyzers and checks every
`SupportedDiagnostics` entry against these anchors and the release inventory.
It validates repository files locally; it does not assert hosted HTTP availability
before merge or real-editor behavior. Keep the ID headings stable when changing
wording. New causes under an existing ID need their supported fixes here too.
The [shared compiler/runtime reason inventory](../test/Htmxor.Tests/Generators/SharedRejectionReasons.md)
records paired evidence and intentional one-sided rules for maintainers; this
page is the canonical user-facing cause/fix reference.
