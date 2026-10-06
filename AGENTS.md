# Toshal.Template: guide for AI agents and contributors

C# class library (.NET 10): a text template engine with ASP style tags (`<%=Name%>`, IF, FOREACH, WITH, SET, ...). Owner: Toshal Infotech. License: MIT.
Repo: https://github.com/AgrawalAshishS/Toshal.Template. Default branch: `main`. NuGet package id: `Toshal.Template`.

## Layout

```
Toshal.Template.sln
src/Toshal.Template/                  the library (NuGet package Toshal.Template)
src/Toshal.Template.Compiled/         run time of compiled .ctt templates: CompiledTemplate (NuGet package Toshal.Template.Compiled).
                                      Uses internals of Toshal.Template (Processing/ProcessRun, TemplateVariables), so both write the same text
src/Toshal.Template.CodeGen/          netstandard2.0: parser and Processor sources of Toshal.Template linked in (CodeGenSources.props) +
                                      emitters that turn a template into C#. Not packed alone. The fixed text of the files they write
                                      (stub, frame of the generated file) is in Templates/*.rtt (run time templates), embedded and run through Processor
src/Toshal.Template.Generator/        MSBuild task GenerateTemplates: writes Name.g.cs and the stub Name.cs next to each .ctt before the compiler
                                      runs; compiles the CodeGen sources in, no Roslyn (NuGet Toshal.Template.Generator, dll in tasks/);
                                      build/ holds the .props/.targets of the package
src/Toshal.Template.Cli/              the toshal-template dotnet tool: writes Name.g.cs and the stub Name.cs to disk (NuGet Toshal.Template.Cli)
src/Directory.Build.props             shared NuGet metadata
tests/Toshal.Template.Tests/          xUnit tests, offline
  Fixes/                              one test file per fixed bug
  KnownIssues/                        skipped tests that show the wanted behavior of open issues
  Examples/ExamplesTests.cs           runs every example
  DocClaimsTests.cs                   proves the behavior that the XML docs describe
  DocsTests.cs                        fails when docs/ is out of date
tests/Toshal.Template.Compiled.Tests/ compiled templates: every template runs through Processor and through its Roslyn-compiled class and
                                      the texts must be equal; MSBuild task (GeneratorTests, StubTests) and tool tests
  Golden/                             the output of the emitters byte for byte; TOSHAL_UPDATE_GOLDEN=1 rewrites it after a wanted change
examples/Toshal.Template.Examples/    console app, one file per topic; Templates/ holds embedded .rtt templates; CompiledTemplates/ holds .ctt
                                      templates, their generated Name.g.cs and filled-in stubs (made by the task of this repository);
                                      Patterns/ + FakeOrRealTestPatternExample.cs: the fake or real database pattern (NpgsqlCommon)
tools/Toshal.Template.Tools/          C# tool: website generator and coverage report. content/ holds the hand written page texts
tools/VisualEditor/                   old browser based template editor (JavaScript). Not built, not maintained yet
benchmarks/Toshal.Template.Benchmarks/ BenchmarkDotNet: the same template through the old chain style, ContextProviderRegistry, a
                                      hand written lower bound and as a compiled template (CompiledShop.ctt). Not packed. Run before and after
                                      any change to Processor, the registry or the code generator
docs/                                 GitHub Pages site. known-issues.md is hand written, everything else is generated
coverlet.runsettings                  coverage settings
.github/workflows/ci.yml              GitHub Actions: build (warnings fail), tests, coverage >= 90%, docs check, pack;
                                      a Linux job runs the pattern example against a PostgreSQL service
```

## Commands (Windows, from the repository root)

| Command | What it does |
|---|---|
| `build` | Build the solution in Release. Prints errors and a summary. |
| `test [filter]` | Run the tests. The optional filter is a `dotnet test --filter` expression. |
| `coverage` | Run the tests with coverlet, print a per-class table, write `coverage/index.html`. Fails below 90%. |
| `docs` | Regenerate the website in `docs/`. `docs check` fails when `docs/` is out of date (a test does the same). |
| `pack` | Build the NuGet package into `artifacts/`. |
| `dotnet run -c Release --project benchmarks/Toshal.Template.Benchmarks -- --filter *` | Measure time and allocations. Run time speed comes first in this library. |

## Rules

1. **Do not change how a public method behaves** without the owner's approval. XML comments, tests, examples, docs and packaging are fine.
2. **Do not change the template syntax** (what a template author writes) without the owner's confirmation. Changing how a template is handled is allowed only to fix an approved bug.
3. **Found a bug? Do not fix it.** Add a skipped test in `tests/Toshal.Template.Tests/KnownIssues/` that shows the wanted behavior, a row in `docs/known-issues.md`, and a "Known issue" remark in the XML docs of the member. Ask the owner.
4. **If a test and the code disagree,** do not pick a side. Write the case down and ask the owner.
5. **Test first.** For every new use case write the test, run it, and change the code only if it fails. Look for an existing test or example first.
6. **Every public member needs XML docs:** summary, every param, returns, exceptions and a short example. Escape `<` and `>`. Use a `<para><b>Warning:</b> ...</para>` remark for traps. The build must have no warnings.
7. **Do not write behavior as fact** unless the code or a test confirms it. Add a test to `DocClaimsTests.cs` for each new claim. If you are not sure, say "I am not sure".
8. **Tests run offline.** The only database test (`ExamplesTests.PatternWithDatabase`) runs only when the environment variable `ConnectionStrings__testdb` points to an empty PostgreSQL database.
9. **Do not edit generated files** in `docs/` by hand (only `docs/known-issues.md`). Change the source and run `docs`.
10. **Nullable stays on.** Fix warnings with annotations; do not add checks or change initial values for it.
11. Commit messages: short imperative subject, one topic per commit. No `Co-Authored-By` or other attribution lines. Do not push; the owner merges and publishes.
12. Bump `VersionPrefix` in `src/Toshal.Template/Toshal.Template.csproj` only when the owner gives the release number.
13. Ask before installing tools or packages.

## Style

Plain, everyday English in docs and comments. Match the surrounding code. C# files use CRLF line endings (see `.gitattributes`); some tests depend on it.
