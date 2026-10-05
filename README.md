# Toshal.Template

A text template engine for .NET with ASP style tags. It produces any text: emails, reports, source code, project files.

```csharp
var tokens = new Parser().Parse("Hello <%=Name%>!");
var processor = new Processor { TokenValueProvider = args => args.Name == "name" ? "World" : null };
string text = processor.Process(new ProcessorArgs(tokens)).ToString();   // Hello World!
```

License: MIT. Copyright (c) 2026 Toshal Infotech.
