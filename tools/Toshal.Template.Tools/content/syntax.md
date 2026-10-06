# Template syntax

Every tag is written as `<%...%>`. Keywords such as `IF` and `FOREACH` are upper case. Names are not case sensitive; the parser lower cases them.
Text outside tags is written as it is, including spaces and line breaks.

| Tag | What it does | Provider |
|---|---|---|
| `<%=Name%>` | Writes a value. A SET variable with that name wins over the provider. Null or empty writes nothing. | `TokenValueProvider` |
| `<%=Name format="0.00"%>` | A value with attributes. Values in quotes may contain spaces. Attribute values keep their case, except in IF, ELSEIF and FOREACH tags, where they are lower cased. | `TokenValueProvider` reads `args.GetAttribute("format", "")` |
| `<%IF Name%>...<%ENDIF%>` | Writes the inner part when the condition is true. `THEN` at the end is optional: `<%IF Name THEN%>`. | `ConditionValueProvider` |
| `<%IF not Name%>` | A negative condition. The provider gets `name` and returns its value; the processor applies the `not`. | `ConditionValueProvider` |
| `<%ELSEIF Name%>`, `<%ELSE%>` | Further branches of an IF. | `ConditionValueProvider` |
| `<%FOREACH Name%>...<%ENDFOR%>` | Writes the inner part once per row. | `LoopValueProvider` returns an `IList` |
| `<%REUSE_FOREACH Existing NewName%>` | Runs the layout of the FOREACH named `Existing` again, for the list named `NewName`. | `LoopValueProvider` |
| `<%WITH Name%>...<%ENDWITH%>` | Makes an object the context of the inner part. Null skips the part. | `WithValueProvider` |
| `<%SET Name%>...<%ENDSET%>` | Writes the inner part into a variable instead of the output. | none |
| `<%PROCESS_TEMPLATE Name%>` | Runs a sub template in place, with the current context. | `ProcessTemplateValueProvider` returns tokens |
| `<%CONTEXT_AS_STRING%>` | Writes `ToString()` of the current context. | none |
| `<%REMOVE_PREVIOUS n%>` | Removes the last n characters written so far. | none |
| `<%REMOVE_PREVIOUS_NEW_LINE%>` | Removes a `\n` at the end of the output, then a `\r` at the end. | none |

## FOREACH parts

Inside a FOREACH you can write these parts, each at most once, each closed by its END tag (for example `<%HEADER%>...<%ENDHEADER%>`).
Text outside every part belongs to ROW.

| Part | When it is written | Context |
|---|---|---|
| `NORECORD` | Instead of everything else, when the list is null or empty. | The context around the loop |
| `HEADER`, `FOOTER` | Once before and after the rows. | The list |
| `BEFOREROW`, `ROW`, `AFTERROW` | For each row. | The row |
| `BEFOREALTROW`, `ALTROW`, `AFTERALTROW` | Instead of the normal parts on the 2nd, 4th, ... row, when not empty. | The row |
| `BEFOREFIRSTROW`, `FIRSTROW`, `AFTERFIRSTROW` | Instead of the normal parts on the first row, when the list has more than one row. | The row |
| `BEFORELASTROW`, `LASTROW`, `AFTERLASTROW` | Instead of the normal parts on the last row. A list with one row uses these. | The row |

## Where SET variables are visible

- A SET at the top, or inside IF, is visible to everything after it.
- A SET inside WITH, PROCESS_TEMPLATE, HEADER, FOOTER, NORECORD, BEFOREROW or AFTERROW stays inside that part.
- A SET inside a row part is visible to the rest of the row, to AFTERROW, and to the later rows of the same loop, but not after the loop.

## Errors

`Parser.Parse` throws a `ParserException`, or one of its subclasses, for a broken template. Each has `LineNumber` and `StartingPosition` (1 based).
See [Parser errors](examples/ParserErrorsExample.html).
