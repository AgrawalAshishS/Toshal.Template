# Contributing

Thank you for helping. Please read [AGENTS.md](AGENTS.md) first: it has the layout, the commands and the rules for everyone, people and AI agents alike.

## Before you open a pull request

GitHub Actions (`.github/workflows/ci.yml`) runs the same checks on every pull request to `main`. Run them locally first:

1. Run `build`, `test` and `coverage` from the repository root. All must pass, with no compiler warnings and line coverage of at least 90%.
2. If you changed XML comments, examples or the pages in `tools/Toshal.Template.Tools/content`, run `docs` and commit the files it changes in `docs/`. A test fails when `docs/` is out of date.
3. Keep one topic per commit, with a short subject in the imperative ("Fix ...", "Add ...").

## Rules in short

- Do not change how a public method behaves without the owner's approval. Do not change the template syntax without the owner's approval.
- Found a bug? Add a skipped test with the wanted behavior, a row in `docs/known-issues.md`, and a "Known issue" remark in the XML docs. Then open an issue.
- Write the test first for every new use case. Look for an existing test or example first.
- Tests must run offline. The database test of the pattern example runs only when `ConnectionStrings__testdb` is set.
- Every public member has XML docs: summary, every param, returns, exceptions and a short example.

## Reporting issues

Open an issue on [GitHub](https://github.com/AgrawalAshishS/Toshal.Template/issues) with the template text, the provider code and the output you expected.
