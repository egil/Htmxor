# Enabling Template Fragments with Conditional Rendering

In Htmxor, conditional rendering supports the [template fragments](https://htmx.org/essays/template-fragments/) pattern.

It allows a single routable component to render specific parts for particular requests or the full content for others. This way, you can keep all related fragments within a single component, avoiding the need to split them into separate, individually routable components.

By consolidating the HTML into one file, it becomes easier to understand feature functionality, adhering to the [Locality of Behavior](https://htmx.org/essays/locality-of-behaviour/) design principle.

