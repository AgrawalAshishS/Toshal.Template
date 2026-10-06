// Title: Pattern 2 of 4: the code under test (production code)
// Summary: A normal service. It reads an order and its lines through an IDatabase, then fills the embedded OrderConfirmation.rtt template. Nothing in it knows about tests.

using System.Data.Common;
using Npgsql;
using NpgsqlCommon;
using Toshal.Template.Examples.Support;

namespace Toshal.Template.Examples.Patterns;

public record OrderLine(string Product, int Quantity, decimal UnitPrice)
{
    public decimal LineTotal => Quantity * UnitPrice;
}

public record Order(int OrderId, string CustomerName, DateTime PlacedOn, List<OrderLine> Lines)
{
    public decimal Total => Lines.Sum(l => l.LineTotal);
}

public sealed class OrderEmailService
{
    private readonly IDatabase db;

    // In production: new OrderEmailService(factory.For("orders")). In a unit test: a TestableDatabase. Nothing else changes.
    public OrderEmailService(IDatabase db)
    {
        this.db = db;
    }

    /// <summary>The confirmation text of an order, or null when the order does not exist.</summary>
    public async Task<string?> RenderConfirmationAsync(int orderId, CancellationToken cancellationToken)
    {
        Order? order = await LoadAsync(orderId, cancellationToken);
        if (order == null) return null;

        var args = new ProcessorArgs(EmbeddedTemplates.Tokens("OrderConfirmation.rtt")) { Context = order };
        return ObjectProviders.Create().Process(args).ToString();
    }

    private async Task<Order?> LoadAsync(int orderId, CancellationToken cancellationToken)
    {
        int id;
        string customer;
        DateTime placedOn;
        await using (DbDataReader reader = await db.ReaderAsync(OrderQueries.GetOrder, [new NpgsqlParameter("order_id", orderId)], cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken)) return null;
            id = reader.GetInt32(reader.GetOrdinal("order_id"));
            customer = reader.GetString(reader.GetOrdinal("customer_name"));
            placedOn = reader.GetDateTime(reader.GetOrdinal("placed_on"));
        }

        var lines = new List<OrderLine>();
        await using (DbDataReader reader = await db.ReaderAsync(OrderQueries.GetLines, [new NpgsqlParameter("order_id", orderId)], cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                lines.Add(new OrderLine(
                    reader.GetString(reader.GetOrdinal("product")),
                    reader.GetInt32(reader.GetOrdinal("quantity")),
                    reader.GetDecimal(reader.GetOrdinal("unit_price"))));
            }
        }

        return new Order(id, customer, placedOn, lines);
    }
}
