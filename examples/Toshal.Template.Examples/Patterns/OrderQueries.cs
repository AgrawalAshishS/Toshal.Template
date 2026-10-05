// Title: Pattern 1 of 4: the queries (production code)
// Summary: The SQL lives in one place as constants. The production code and the fake setup both use these constants, so a changed query cannot drift away from its test setup.

using NpgsqlCommon;

namespace Toshal.Template.Examples.Patterns;

public static class OrderQueries
{
    // {SCHEMA} and {PREFIX} are replaced by NpgsqlCommon with the schema and prefix of the database scope.
    public const string GetOrder = "select order_id, customer_name, placed_on from {SCHEMA}{PREFIX}sample_orders where order_id = @order_id";

    public const string GetLines = "select product, quantity, unit_price from {SCHEMA}{PREFIX}sample_order_lines where order_id = @order_id order by line_no";
}

public static class OrderScopes
{
    /// <summary>The scope rule of the order service: one database called "main", no schema and no prefix. Production and tests use the same rule.</summary>
    public static DbScope Rule(in DbKey key) => new DbScope("main");

    /// <summary>The text of a query after the markers are replaced. This is the text the fake rules see.</summary>
    public static string Final(string template) => Rule(new DbKey("orders")).Apply(template);
}
