// Title: Pattern 4 of 4: one scenario, fake or real database (test code)
// Summary: The complete pattern. The same scenario runs against fakes (unit test, no server) or against PostgreSQL (integration test). Only the arrange and verify steps differ; the act and the checks on the email text are shared, which proves the fake behaves like the database.

using Npgsql;
using NpgsqlCommon;
using NpgsqlCommon.Testable;
using Toshal.Template.Examples.Patterns;
using Toshal.Template.Examples.Support;

namespace Toshal.Template.Examples;

public enum TestMode
{
    /// <summary>Unit test: TestableDatabaseFactory. Fast, needs no server.</summary>
    Fake = 1,

    /// <summary>Integration test: NpgsqlDatabaseFactory against a real PostgreSQL database.</summary>
    Database = 2,
}

public static class FakeOrRealTestPatternExample
{
    private const string Expected =
        "Order 7 for Asha Patel, placed on 2026-03-05\n" +
        "2 x Pen at 10.50 = 21.00\n" +
        "1 x Notebook at 45.00 = 45.00\n" +
        "Total: 66.00";

    // Scenario: an order with two lines, an order without lines, and an order that does not exist.
    // In your test project this body is the test method, and the Verify calls are your assertions.
    public static async Task ConfirmationEmails(TestMode mode, string? connectionString = null)
    {
        TestableDatabaseFactory? fake = null;
        DataSourceRegistry? sources = null;
        IDatabase db;

        // ---- ARRANGE: the only part that differs a lot between the modes.
        if (mode == TestMode.Fake)
        {
            fake = new TestableDatabaseFactory(OrderScopes.Rule);

            // A METHOD per rule, so every call gets a fresh reader, and the answer depends on the parameter like a table would.
            OrderQueryFakes.SetupGetOrder(fake).ReturnsDefault((call, parameters) =>
            {
                var result = OrderQueryFakes.NewGetOrderResult();
                int id = (int)parameters!.Single(p => p.ParameterName == "order_id").Value!;
                if (id == 7) result.AddRow(7, "Asha Patel", new DateTime(2026, 3, 5));
                if (id == 8) result.AddRow(8, "Ravi Shah", new DateTime(2026, 3, 6));
                return new TestableDataReader(result);
            });

            OrderQueryFakes.SetupGetLines(fake).ReturnsDefault((call, parameters) =>
            {
                var result = OrderQueryFakes.NewGetLinesResult();
                if ((int)parameters!.Single(p => p.ParameterName == "order_id").Value! == 7)
                {
                    result.AddRow("Pen", 2, 10.50m);
                    result.AddRow("Notebook", 1, 45.00m);
                }
                return new TestableDataReader(result);
            });

            db = fake.For("orders");
        }
        else
        {
            // The same scope rule. The registry turns the name "main" into the connection string.
            sources = new DataSourceRegistry(new ConnectionStringMap { ["main"] = connectionString! });
            db = new NpgsqlDatabaseFactory(OrderScopes.Rule, sources).For("orders");
            await DropTablesAsync(db);
            await db.NonQueryAsync("create table sample_orders (order_id int primary key, customer_name text not null, placed_on date not null)");
            await db.NonQueryAsync("create table sample_order_lines (order_id int not null, line_no int not null, product text not null, quantity int not null, unit_price numeric(10,2) not null)");
            await db.NonQueryAsync("insert into sample_orders values (7, 'Asha Patel', '2026-03-05'), (8, 'Ravi Shah', '2026-03-06')");
            await db.NonQueryAsync("insert into sample_order_lines values (7, 1, 'Pen', 2, 10.50), (7, 2, 'Notebook', 1, 45.00)");
        }

        try
        {
            // ---- ACT: identical in both modes.
            var service = new OrderEmailService(db);
            string? withLines = await service.RenderConfirmationAsync(7, CancellationToken.None);
            string? noLines = await service.RenderConfirmationAsync(8, CancellationToken.None);
            string? missing = await service.RenderConfirmationAsync(999, CancellationToken.None);

            // ---- ASSERT on the result: identical in both modes.
            Verify.Equal(Expected, withLines ?? "(null)", "order with lines");
            Verify.Equal("Order 8 for Ravi Shah, placed on 2026-03-06\nThis order has no lines yet.", noLines ?? "(null)", "order without lines");
            Verify.That(missing == null, "unknown order gives null");

            // ---- VERIFY: each mode proves its own side.
            if (mode == TestMode.Fake)
            {
                Verify.That(fake!.AllQueryGotCalled(), "all rules used");
                Verify.That(fake.Calls.Count == 5, "two calls per found order, one for the missing order");
                Verify.That(fake.Calls[0].Template == OrderQueries.GetOrder && fake.Calls[1].Template == OrderQueries.GetLines, "the order is read before its lines");
            }
            else
            {
                Verify.That(await db.ScalarAsync<long>("select count(*) from sample_order_lines") == 2, "the lines are in the table");
            }
        }
        finally
        {
            if (mode == TestMode.Database)
            {
                await DropTablesAsync(db);
                await sources!.DisposeAsync();
            }
        }
    }

    private static async Task DropTablesAsync(IDatabase db)
    {
        await db.NonQueryAsync("drop table if exists sample_order_lines");
        await db.NonQueryAsync("drop table if exists sample_orders");
    }

    public static async Task Run(string? connectionString)
    {
        Console.WriteLine("== One scenario, fake or real database ==");

        await ConfirmationEmails(TestMode.Fake);
        Console.WriteLine("Fake mode: passed.");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.WriteLine("Database mode: skipped (set the environment variable ConnectionStrings__testdb).");
            return;
        }

        await ConfirmationEmails(TestMode.Database, connectionString);
        Console.WriteLine("Database mode: passed.");
    }
}
