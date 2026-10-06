# Why use it

**Templates stay plain text.** A template is a text file with a few tags. People who do not write C# can read and change it, and a diff shows exactly what changed.

**Any output.** The engine does not know about HTML, so it does not escape anything and does not add anything. The same tags write an email, a fixed width report, a C# class, a SQL script or a `.csproj` file.

**You decide what a name means.** There is no reflection and no expression language inside the engine. Five small provider functions answer the questions the template asks: the text of a value, the truth of a condition, the rows of a loop, the object of a WITH block, and the tokens of a sub template. That keeps the engine small and the rules in your code, where you can test them. If you want property lookup by name, the examples have a short helper (`Support/ObjectProviders.cs`).

**Many context types stay fast.** With `ContextProviderRegistry` you write one `ContextProvider<T>` per type of context. The registry picks the providers by the type of the context with one cached lookup, so no provider checks the type and no long chain of providers runs for every tag. It uses no reflection while processing.

**Parse once, run many times.** `Parser.Parse` returns tokens that the processor does not change, so you can parse a template at start up and reuse the tokens for every email or report.

**Loops have the parts that reports need.** A FOREACH can have a header, a footer, a "no rows" text, alternating rows, and special first and last rows, without counters in your code.

**Clear errors.** A broken template fails in `Parser.Parse` with an exception that names the line and column.

## When not to use it

- You need HTML escaping or a full expression language in the template. Use a web view engine for HTML pages.
- You need templates that call arbitrary code. This engine only asks your providers.
