// Title: Pattern 3 of 4: one setup helper per query (test code)
// Summary: A small static helper returns the fake rule for one query, and another builds an empty result with the right columns. Every test starts from them and adds only what is special.

using System.Data.Common;
using NpgsqlCommon.Testable;

namespace Toshal.Template.Examples.Patterns;

public static class OrderQueryFakes
{
    /// <summary>The rule for OrderQueries.GetOrder. Add ReturnsDefault or ForCallNumber to it.</summary>
    public static TestableQueryHandler<DbDataReader>.ResultManagement<DbDataReader> SetupGetOrder(TestableDatabaseFactory dbs)
    {
        return dbs.When().ExecuteReaderCalled().ForQueryEquals(OrderScopes.Final(OrderQueries.GetOrder));
    }

    /// <summary>The rule for OrderQueries.GetLines.</summary>
    public static TestableQueryHandler<DbDataReader>.ResultManagement<DbDataReader> SetupGetLines(TestableDatabaseFactory dbs)
    {
        return dbs.When().ExecuteReaderCalled().ForQueryEquals(OrderScopes.Final(OrderQueries.GetLines));
    }

    /// <summary>An empty result with the columns and the PostgreSQL types of OrderQueries.GetOrder.</summary>
    public static TestableResult NewGetOrderResult()
    {
        return new TestableResult()
            .AddColumn("order_id", typeof(int))
            .AddColumn("customer_name", typeof(string))
            .AddColumn("placed_on", typeof(DateTime));
    }

    /// <summary>An empty result with the columns and the PostgreSQL types of OrderQueries.GetLines.</summary>
    public static TestableResult NewGetLinesResult()
    {
        return new TestableResult()
            .AddColumn("product", typeof(string))
            .AddColumn("quantity", typeof(int))
            .AddColumn("unit_price", typeof(decimal));
    }
}
