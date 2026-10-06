# Known issues

This page is written by hand. Each row has a test in `tests/Toshal.Template.Tests/KnownIssues/KnownIssueTests.cs`
or `tests/Toshal.Template.Tests/Fixes/`. A skipped test shows the wanted behavior; the other tests pin what the library does today.

## Open

None at the moment.

## By design

| Behavior | Test |
|---|---|
| IF, ELSEIF and FOREACH tags lower case their attribute values: `<%IF Weight unit="KG"%>` gives `kg`. Value tags, WITH, SET and PROCESS_TEMPLATE keep the case. | `KnownIssues/KnownIssueTests.IfAndForEachAttributeValuesAreLowerCased` |

## Open questions

These are not clearly bugs. The owner has to decide what the template language should do.

| Question | Today | Test |
|---|---|---|
| A list with one row: FIRSTROW, LASTROW or both? | The LAST parts (BEFORELASTROW, LASTROW, AFTERLASTROW) are used; the FIRST parts are not. | `OneRowUsesTheLastRowPartToday` |
| `<%REUSE_FOREACH a b%>` when two FOREACH blocks are named `a`. | `Parser.Parse` throws `ArgumentException` ("FOREACH name a is used more than once"). Should it be a `ParserException`, or should the first or last block win? | `ReuseOfADuplicateForEachNameFails` |

## Fixed in this version

| Issue | Test |
|---|---|
| A SET inside AFTERROW changed the variables around the loop, and AFTERROW did not see the variables set in ROW. | `Fixes/AfterRowSetScopeTests` |
| An ELSEIF with no content skipped the following ELSEIF and ELSE. | `Fixes/EmptyElseIfTests` |
| A template that ended with `<%ELSE%>` threw `ArgumentOutOfRangeException` instead of `TokenNotClosedException`, and `ElseToken` had the position of the text after the ELSE tag. | `Fixes/ElseAtEndTests` |
| A `Parser` instance kept the FOREACH blocks of earlier templates, so a second `Parse` with REUSE_FOREACH failed or linked to an old block. Two FOREACH blocks with the same name broke every REUSE_FOREACH in the template. | `Fixes/ParserReuseTests` |
| A `%>` with no text before it (at the start, or right after a tag) was lost. | `Fixes/StrayCloseTagTests` |
| Line and column numbers: the first text had 0 and 0, columns after a line break were 0 based, and text after a tag pointed to the `%>`. All are 1 based now. | `Fixes/TokenPositionTests` |
| `new TokenArgs(args, context)`, `new ConditionArgs(args, context)` and `new LoopArgs(args, context)` added to the parent context list of the original args. | `Fixes/ArgsCopyTests` |
| Two loop items that are equal (for example the same string in an outer and an inner loop) removed the wrong entry from `ParentContext`. | `Fixes/ParentContextStackTests` |
| Attribute values with spaces, and attributes separated by more than one space, threw `InvalidTokenAttributeException`. | `Fixes/AttributeValueSpaceTests` |
| A negative count in `<%REMOVE_PREVIOUS n%>` was accepted by the parser, and the processor then threw `ArgumentOutOfRangeException`. The parser now throws `ParserException`. | `Fixes/NegativeRemovePreviousTests` |
| The same attribute twice in one tag, such as `<%=Name a="1" a="2"%>`, threw `ArgumentException`. It now throws `InvalidTokenAttributeException`. | `Fixes/AttributeValueSpaceTests.RepeatedAttributeIsInvalid` |
