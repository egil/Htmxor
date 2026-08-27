# Code-comment policy

Comments are maintained design records. They must explain information that names, types, checks, and control flow cannot state clearly.

Prefer clear code first. Refactor avoidable confusion before writing prose, unless the refactor is outside the authorized scope. Do not narrate syntax or restate a declaration's name.

Add a local comment when a maintainer would otherwise have to reconstruct:

- a correctness, security, compatibility, lifecycle, ordering, or performance constraint;
- a non-obvious assumption that types or executable checks cannot enforce;
- behavior that looks suspicious but is deliberate;
- why a plausible alternative would break a Blazor, HTMX, HTTP, or tooling contract;
- the strategy and load-bearing invariant of a non-obvious algorithm.

Explain the cause or constraint and place the comment next to the code it governs. Use XML documentation for supported caller-visible contracts and ordinary `//` comments for implementation rationale. In C#, write implementation comments on their own line with sentence case and punctuation.

Tests are executable documentation. Name them after the observable scenario and outcome. Use whitespace for obvious Arrange, Act, and Assert sections. Add a short semantic comment only when an unusual fixture, sentinel, indirect assertion, or boundary makes the causal intent hard to see.

Update or remove a comment with the code it describes. A TODO must link to tracked context and state the concrete action or removal condition. Do not use a person's name as the ownership mechanism.
