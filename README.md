# Toshal.Template

A text template engine for .NET 10 with ASP style tags. It produces any text: emails, reports, source code, SQL scripts, project files.

```text
Hi <%=FirstName%>,
<%IF IsTrial%>Your trial ends on <%=TrialEnds format="dd MMM yyyy"%>.<%ENDIF%>
<%FOREACH Lines%>  - <%=Quantity%> x <%=Product%>
<%NORECORD%>No lines yet.<%ENDNORECORD%><%ENDFOR%>
```

Parse a template once, then process it as often as you like. Your own provider functions supply the data:

```csharp
var tokens = new Parser().Parse("Hello <%=Name%>!");
var processor = new Processor { TokenValueProvider = args => args.Name == "name" ? "World" : null };
string text = processor.Process(new ProcessorArgs(tokens)).ToString();   // Hello World!
```

## Tags

`<%=Name%>` values (with attributes such as `format="0.00"`), `IF` / `ELSEIF` / `ELSE` / `not`, `FOREACH` with header, footer, alternating, first, last and "no rows" parts, `REUSE_FOREACH`, `WITH`, `SET` variables, `PROCESS_TEMPLATE` sub templates, `CONTEXT_AS_STRING`, and `REMOVE_PREVIOUS` / `REMOVE_PREVIOUS_NEW_LINE` for whitespace control.

## Documentation

- Website: https://agrawalashishs.github.io/Toshal.Template/ (generated from `docs/`)
- Runnable examples: `dotnet run --project examples/Toshal.Template.Examples`
- Known issues: [docs/known-issues.md](docs/known-issues.md)
- Contributing: [CONTRIBUTING.md](CONTRIBUTING.md)

License: MIT. Copyright (c) 2026 Toshal Infotech.
