# Getting started

## Install

```text
dotnet add package Toshal.Template
```

The library targets .NET 10 and has no dependencies.

## A first template

{{include:examples/Toshal.Template.Examples/QuickStartExample.cs}}

## The three steps

1. **Parse** the template text with `new Parser().Parse(text)`. It returns a `List<IToken>`. A broken template throws a `ParserException` with the line and column. Parse once and keep the tokens.
2. **Set the providers** on a `Processor`. Each provider gets an args object with the tag name in lower case (`args.Name`), the tag attributes (`args.Attributes`), the current object (`args.Context`) and the objects around it (`args.ParentContext`).
3. **Process** with `processor.Process(new ProcessorArgs(tokens) { Context = data })`. It returns a `StringBuilder`.

## Things to know early

- Names are lower cased by the parser. `<%=FirstName%>` arrives as `firstname`.
- A provider that is not set makes its tags disappear silently. For example, without a condition provider every IF block is skipped, ELSE included.
- A value provider that returns null or an empty string writes nothing.
- Keep templates in files with the extension `.rtt` (run time Toshal template). The examples embed them in the assembly; see [Helper: templates as embedded files](examples/EmbeddedTemplates.html).

## Next

- [Template syntax](syntax.html)
- [Examples](examples/index.html)
- [API reference](api/index.html)
