// Title: WITH, the context and the parent contexts
// Summary: WITH switches the context to a child object. Providers see the current object in args.Context and the outer ones in args.ParentContext. The args copy constructors hand a part of the work to another provider.

using Toshal.Template.Examples.Support;

namespace Toshal.Template.Examples;

public static class WithAndParentContextExample
{
    private record Address(string City);
    private record Customer(string Name, Address? Address);
    private record Order(int Id, Customer Customer);

    public static void Run()
    {
        Console.WriteLine("== WITH and parent contexts ==");

        const string template = "Order <%=Id%>: <%WITH Customer%><%=Name%><%WITH Address%> from <%=City%> (order <%=Id%>)<%ENDWITH%><%ENDWITH%>";
        var tokens = new Parser().Parse(template);

        var processor = new Processor
        {
            WithValueProvider = args => args.Context switch
            {
                Order o when args.Name == "customer" => o.Customer,
                Customer c when args.Name == "address" => c.Address,   // null skips the WITH block
                _ => null,
            },
            TokenValueProvider = args => args.Context switch
            {
                Order o => o.Id.ToString(),
                Customer c => c.Name,
                // Inside the address, "id" is not on the context: look in the parent contexts.
                Address a when args.Name == "city" => a.City,
                Address when args.Name == "id" => args.ParentContext.OfType<Order>().Last().Id.ToString(),
                _ => null,
            },
        };

        string Render(Order o) => processor.Process(new ProcessorArgs(tokens) { Context = o }).ToString();

        var withAddress = new Order(7, new Customer("Asha", new Address("Surat")));
        Console.WriteLine(Render(withAddress));
        Verify.Equal("Order 7: Asha from Surat (order 7)", Render(withAddress), "with");
        Verify.Equal("Order 8: Ravi", Render(new Order(8, new Customer("Ravi", null))), "null skips the block");

        // CONTEXT_AS_STRING writes the current context without a provider.
        var tags = new Parser().Parse("<%FOREACH tags%>#<%CONTEXT_AS_STRING%> <%ENDFOR%>");
        Verify.Equal("#news #tech ", new Processor { LoopValueProvider = a => new[] { "news", "tech" } }.Process(new ProcessorArgs(tags)).ToString(), "context as string");

        // Copy constructors: the order provider hands "customername" to a provider that knows customers, with the customer as context.
        Func<TokenArgs, string?> customerProvider = args => args.Context is Customer c && args.ParentContext[^1] is Order ? c.Name : null;
        var delegating = new Processor
        {
            TokenValueProvider = args =>
            {
                if (args.Context is not Order order || args.Name != "customername") return null;
                return customerProvider(new TokenArgs(args, order.Customer));   // a copy: args.ParentContext is not changed
            },
        };
        string delegated = delegating.Process(new ProcessorArgs(new Parser().Parse("<%=CustomerName%>")) { Context = withAddress }).ToString();
        Verify.Equal("Asha", delegated, "TokenArgs copy");

        // The same copy constructors exist for conditions and loops. The child ends its ParentContext with the old context.
        var conditionArgs = new ConditionArgs(new Tokens.ConditionToken(new Split { Content = "<%IF vip%>" }), withAddress, [withAddress]);
        var childCondition = new ConditionArgs(conditionArgs, withAddress.Customer);
        Verify.That(Equals(childCondition.Context, withAddress.Customer) && childCondition.ParentContext.Count == 2 && conditionArgs.ParentContext.Count == 1, "ConditionArgs copy");

        var loopArgs = new LoopArgs("lines", withAddress, [withAddress], new Tokens.TokenAttributeDictionary());
        var childLoop = new LoopArgs(loopArgs, withAddress.Customer);
        Verify.That(childLoop.Name == "lines" && Equals(childLoop.ParentContext[^1], withAddress), "LoopArgs copy");

        Console.WriteLine("Copies leave the original ParentContext alone.");
    }
}
