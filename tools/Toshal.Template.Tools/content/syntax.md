# Template syntax

Every tag is written as `<%...%>`. Keywords such as `IF` and `FOREACH` are upper case. Names are not case sensitive; the parser lower cases them.
Text outside tags is written as it is, including spaces and line breaks, with one exception: a line that holds only control tags writes nothing
(see [Lines with only control tags](#lines-with-only-control-tags)).

| Tag | What it does | Provider |
|---|---|---|
| `<%=Name%>` | Writes a value. A SET variable with that name wins over the provider. Null or empty writes nothing. | `TokenValueProvider` |
| `<%=Name format="0.00"%>` | A value with attributes. Values in quotes may contain spaces. Attribute names are lower case. Attribute values keep their case in every tag; `Attributes.LowerCaseValues` has a lower case copy that the parser makes. | `TokenValueProvider` reads `args.GetAttribute("format", "")` |
| `<%IF Name%>...<%ENDIF%>` | Writes the inner part when the condition is true. `THEN` at the end is optional: `<%IF Name THEN%>`. | `ConditionValueProvider` |
| `<%IF not Name%>` | A negative condition. The provider gets `name` and returns its value; the processor applies the `not`. | `ConditionValueProvider` |
| `<%ELSEIF Name%>`, `<%ELSE%>` | Further branches of an IF. | `ConditionValueProvider` |
| `<%FOREACH Name%>...<%ENDFOR%>` | Writes the inner part once per row. | `LoopValueProvider` returns an `IList` |
| `<%REUSE_FOREACH Existing NewName%>` | Runs the layout of the nearest FOREACH named `Existing` again, for the list named `NewName`. | `LoopValueProvider` |
| `<%WITH Name%>...<%ENDWITH%>` | Makes an object the context of the inner part. Null skips the part. | `WithValueProvider` |
| `<%SET Name%>...<%ENDSET%>` | Writes the inner part into a variable instead of the output. | none |
| `<%PROCESS_TEMPLATE Name%>` | Runs a sub template in place, with the current context. Alone on an indented line, it indents every line of the sub template (see below). | `ProcessTemplateValueProvider` returns tokens |
| `<%SEPARATOR%>...<%ENDSEPARATOR%>` | Inside a FOREACH row: written in every row except the last, like the separator of `string.Join`. | none |
| `<%CONTEXT_AS_STRING%>` | Writes `ToString()` of the current context. | none |
| `<%REMOVE_PREVIOUS n%>` | Removes the last n characters written so far. For a separator between rows, SEPARATOR is simpler and does not depend on the line breaks. | none |
| `<%REMOVE_PREVIOUS_NEW_LINE%>` | Removes a `\n` at the end of the output, then a `\r` at the end. | none |
| `<%-- note --%>` | A comment. Writes nothing. It may span lines and hold tags and `%>`; it ends at the first `--%>`. | none |
| `\<\%`, `\%\>` | Write `<%` and `%>` as plain text, so `\<\%=Name\%\>` writes `<%=Name%>`. Other backslashes are written as they are. | none |

## Lines with only control tags

A line that holds only control tags and spaces or tabs writes nothing: not its indent, not the spaces between the tags, not its line break.
So you can put each control tag on its own line and indent it like code, and the output has no extra blank lines or spaces.

Control tags are IF, ELSEIF, ELSE, ENDIF, FOREACH, ENDFOR, every FOREACH part and its end (ROW, ENDROW, HEADER, ...), WITH, ENDWITH, SET, ENDSET,
SEPARATOR, ENDSEPARATOR and comments.
A line with any text, or with a tag that writes something (`<%=Name%>`, CONTEXT_AS_STRING, PROCESS_TEMPLATE, REUSE_FOREACH), or with
REMOVE_PREVIOUS or REMOVE_PREVIOUS_NEW_LINE, is written as it is. The first and the last line of the template count too. Both `
` and `
` line breaks work.

```text
public class <%=Name%>
{
    <%FOREACH Columns%>
    public <%=Type%> <%=Name%> { get; set; }
    <%ENDFOR%>
}
```

writes

```text
public class Customer
{
    public int Id { get; set; }
    public string Email { get; set; }
}
```

The parser does this once, when it parses the template, so it costs nothing while processing.

## SEPARATOR

`<%SEPARATOR%>...<%ENDSEPARATOR%>` is written in every row of the innermost FOREACH except the last one. It counts no characters, so unlike
`<%REMOVE_PREVIOUS n%>` it works the same with `\n` and `\r\n` line breaks.

```text
public Customer(
    <%FOREACH Fields%>
    <%=Type%> <%=Name%><%SEPARATOR%>,<%ENDSEPARATOR%>
    <%ENDFOR%>
)
```

writes

```text
public Customer(
    int id,
    string email
)
```

It may stand anywhere in a row: in the ROW, ALTROW, FIRSTROW or LASTROW part, in a BEFORE or AFTER part, or inside an IF or WITH in the row.
In a HEADER, FOOTER or NORECORD part, or outside any FOREACH, it writes nothing. In a sub template that a row runs with PROCESS_TEMPLATE it
belongs to that row.

## Indented sub templates

When `<%PROCESS_TEMPLATE Name%>` stands alone on its line after spaces or tabs, those spaces or tabs indent every line of the sub template.
The sub template keeps its own indent; the outer indent is added on top. Empty lines get no indent, so no line ends with spaces.

```text
    public void Save(Customer customer)
    {
        <%PROCESS_TEMPLATE NullCheck%>
    }
```

with the sub template

```text
if (customer == null)
{
    throw new ArgumentNullException(nameof(customer));
}
```

writes

```text
    public void Save(Customer customer)
    {
        if (customer == null)
        {
            throw new ArgumentNullException(nameof(customer));
        }
    }
```

A sub template that is not alone on its line is written as it is. Inside an indented sub template, REMOVE_PREVIOUS and REMOVE_PREVIOUS_NEW_LINE
remove only text of the sub template.

## FOREACH names and REUSE_FOREACH

A FOREACH name is unique per level. A level is the top of the template, or the inside of an IF, ELSEIF, ELSE, WITH, SET or FOREACH part.
Two FOREACH blocks with the same name at the same level are a `ParserException`; the same name at a deeper level, or in the IF and the ELSE part, is fine.

REUSE_FOREACH uses the nearest FOREACH with its name: its own level first (the FOREACH may come before or after it), then each outer level up to the top.
When none of those levels has the name, it uses the first FOREACH with that name in the whole template, top to bottom (for example one inside an IF next to it).

```text
<%FOREACH items%>top layout<%ENDFOR%>
<%WITH archive%>
  <%FOREACH items%>archive layout<%ENDFOR%>
  <%REUSE_FOREACH items old%>      uses the archive layout (same level)
<%ENDWITH%>
<%REUSE_FOREACH items more%>       uses the top layout
```

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
| `BEFORELASTROW`, `LASTROW`, `AFTERLASTROW` | Instead of the normal parts on the last row. A list with one row uses these, by design: the last row template wins over the first row template. | The row |

## Where SET variables are visible

- A SET at the top, or inside IF, is visible to everything after it.
- A SET inside WITH, PROCESS_TEMPLATE, HEADER, FOOTER, NORECORD, BEFOREROW or AFTERROW stays inside that part.
- A SET inside a row part is visible to the rest of the row, to AFTERROW, and to the later rows of the same loop, but not after the loop.

## Errors

`Parser.Parse` throws a `ParserException`, or one of its subclasses, for a broken template. Each has `LineNumber` and `StartingPosition` (1 based).
See [Parser errors](examples/ParserErrorsExample.html).
