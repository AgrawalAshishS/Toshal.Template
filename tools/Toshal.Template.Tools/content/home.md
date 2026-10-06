# Toshal.Template

A text template engine for .NET 10 with ASP style tags. It produces any text: emails, reports, source code, SQL scripts, project files.

```text
Hi <%=FirstName%>,
<%IF IsTrial%>Your trial ends on <%=TrialEnds format="dd MMM yyyy"%>.<%ENDIF%>
<%FOREACH Lines%>  - <%=Quantity%> x <%=Product%>
<%NORECORD%>No lines yet.<%ENDNORECORD%><%ENDFOR%>
```

You parse a template once with `Parser.Parse`, then `Processor.Process` turns it into text as often as you like.
The data comes from small provider functions that you write, so the engine never guesses how to read your objects.

```csharp
var tokens = new Parser().Parse("Hello <%=Name%>!");
var processor = new Processor { TokenValueProvider = args => args.Name == "name" ? "World" : null };
string text = processor.Process(new ProcessorArgs(tokens)).ToString();   // Hello World!
```

- [Getting started](getting-started.html): install it and write a first template.
- [Template syntax](syntax.html): every tag on one page.
- [Examples](examples/index.html): one runnable file per topic, plus realistic emails, reports and code generation.
- [Testing pattern](pattern.html): one scenario that runs against fakes and against PostgreSQL.
- [Known issues](known-issues.html): what does not work as expected yet.

The package id is `Toshal.Template`. License: MIT.
