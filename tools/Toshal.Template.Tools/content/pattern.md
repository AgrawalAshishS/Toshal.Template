# Testing pattern: one scenario, fake or real database

Code that fills a template usually reads its data from a database first. This page shows how to test such code with one scenario that runs in two modes:

- **Fake mode**: the database is replaced by the fakes of [NpgsqlCommon](https://github.com/AgrawalAshishS/NpgsqlCommon). It is a fast unit test that needs no server.
- **Database mode**: the same scenario runs against a real PostgreSQL database. It is an integration test.

The act step and the checks on the produced text are shared. When both modes pass, the fakes behave like the database for this scenario.

The example has four parts. The first two are production code, the last two are test code.

## 1. The queries

The SQL lives in one place as constants, used by the production code and by the fakes.

{{include:examples/Toshal.Template.Examples/Patterns/OrderQueries.cs}}

## 2. The code under test

A normal service: it reads an order and its lines through `IDatabase`, then fills an embedded template.

{{include:examples/Toshal.Template.Examples/Patterns/OrderEmailService.cs}}

The template:

{{include:examples/Toshal.Template.Examples/Templates/OrderConfirmation.txt}}

## 3. One setup helper per query

{{include:examples/Toshal.Template.Examples/Patterns/OrderQueryFakes.cs}}

## 4. The scenario in both modes

{{include:examples/Toshal.Template.Examples/FakeOrRealTestPatternExample.cs}}

## Running it

- `test.cmd` runs the fake mode every time (`ExamplesTests.PatternWithFakes`).
- The database mode (`ExamplesTests.PatternWithDatabase`) runs only when the environment variable `ConnectionStrings__testdb` points to an empty PostgreSQL database. It creates two tables named `sample_orders` and `sample_order_lines`, and drops them at the end.
