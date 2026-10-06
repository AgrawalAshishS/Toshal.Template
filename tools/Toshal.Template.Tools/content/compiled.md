# Compiled templates

A template can also be turned into a C# class when the project is built. The template text becomes plain C# code: `output.Append("...")`
for text, `if` for IF, a `for` loop for FOREACH. So no parser runs and no token list is walked at run time. You write the template, not the
string building code.

## Install

Use the source generator (the class is made in memory on every build):

```text
dotnet add package Toshal.Template.Generator
```

or the tool (the class is written to disk next to the template):

```text
dotnet tool install --global Toshal.Template.Cli
toshal-template generate Templates
```

Use one of the two in a project, not both: both would make the same class. Both bring the run time package `Toshal.Template.Compiled`.

## Write a template

Give the file the extension `.ctt`. The syntax is the same as for the processor; see [Template syntax](syntax.html).

```text
The <%=Product abc="xyz"%> team
```

The class name is the file name. The namespace is the root namespace of the project and the folders, the way .resx files get theirs:
`Templates/Email/OrderConfirm.ctt` in the project `MyApp` is the class `MyApp.Templates.Email.OrderConfirm`.

With the generator every `.ctt` file of the project folder and its sub folders is used. To list them yourself, set the property
`ToshalTemplateAutoInclude` to `false` and add `<AdditionalFiles Include="Templates\**\*.ctt" />`. The item metadata `ClassName` and
`Namespace` change the names:

```text
<AdditionalFiles Update="Templates\Mail.ctt" ClassName="MailTemplate" Namespace="Shop.Mail" />
```

The tool takes files or folders, and the options `--root-namespace`, `--project-dir`, `--namespace`, `--class`, `--writer` and `--no-stub`.

## Give the values

The generated half of the class declares one partial method for each kind of tag the template uses:

| Tag | Partial method |
|---|---|
| `<%=name%>` | `string? TokenValue(TokenArgs args)`, or `void WriteToken(TokenArgs args, StringBuilder output)` |
| `<%IF name%>`, `<%ELSEIF name%>` | `bool Condition(ConditionArgs args)` |
| `<%FOREACH name%>`, `<%REUSE_FOREACH existing name%>` | `IList? Loop(LoopArgs args)` |
| `<%WITH name%>` | `object? With(TokenArgs args)` |
| `<%PROCESS_TEMPLATE name%>` | `CompiledTemplate? SubTemplate(ProcessTemplateArgs args)` |

The other half is made for you once, next to the template (`OrderConfirm.cs` beside `OrderConfirm.ctt`), with a case for every name the
template uses. It is yours: fill in the values. It is never made again. When the template later uses a new kind of tag, the compiler names
the method to add. To use `WriteToken` instead of `TokenValue`, rename the method in your half (with the tool, also pass `--writer` once).

The args are the same as for the processor, so the values are found the same way: `args.Name` in lower case, `args.Attributes`,
`args.Context` and `args.ParentContext`. A method that switches on the type of `args.Context` and then on `args.Name` needs no reflection.

## Run it

```text
string text = new OrderConfirm().Process(order).ToString();
```

`Process(context, output)` appends to a builder you give, as `Processor.Process(args, output)` does. One instance can be used from many threads
at once, as long as your partial methods can.

## Same text as the processor

A compiled template writes exactly the text the processor writes for the same template and the same provider answers: the same SET scopes,
FOREACH parts, SEPARATOR, REMOVE_PREVIOUS, indents of sub templates and lines that hold only control tags. The tests run every template of the
parser tests through both and compare the text.

Differences: a sub template is another compiled class, returned by `SubTemplate`, not a token list. A template with an error does not build:
the generator reports `TTC001` at the line and column of the error, and the tool prints the same.

{{include:examples/Toshal.Template.Examples/CompiledTemplatesExample.cs}}
