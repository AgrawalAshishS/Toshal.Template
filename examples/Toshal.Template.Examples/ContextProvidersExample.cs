// Title: Typed providers with ContextProviderRegistry
// Summary: Many context types? Write one ContextProvider<T> per type and register them. The registry picks the providers by the type of the context, so no provider has to check the type, and a hand written provider can win over a generated one, even with false or an empty text.

using System.Collections;
using Toshal.Template.Examples.Support;
using Toshal.Template.Providers;

namespace Toshal.Template.Examples;

public static class ContextProvidersExample
{
    private sealed class Table
    {
        public string Name { get; init; } = "";

        public List<Column> Columns { get; } = new();
    }

    private sealed class Column
    {
        public string Name { get; init; } = "";

        public string Type { get; init; } = "";

        public bool IsNullable { get; init; }

        public bool IsKey { get; init; }
    }

    // One case per property. A code generator could write this class; there is no reflection at run time.
    private sealed class TableProvider : ContextProvider<Table>
    {
        public override bool TryToken(Table context, TokenArgs args, out string? value)
        {
            switch (args.Name)
            {
                case "name": value = context.Name; return true;
            }

            value = null;
            return false;
        }

        public override bool TryLoop(Table context, LoopArgs args, out IList? value)
        {
            switch (args.Name)
            {
                case "columns": value = context.Columns; return true;
            }

            value = null;
            return false;
        }
    }

    private sealed class ColumnProvider : ContextProvider<Column>
    {
        public override bool TryToken(Column context, TokenArgs args, out string? value)
        {
            switch (args.Name)
            {
                case "name": value = context.Name; return true;
                case "type": value = context.Type; return true;
            }

            value = null;
            return false;
        }

        public override bool TryCondition(Column context, ConditionArgs args, out bool value)
        {
            switch (args.Name)
            {
                case "is_nullable": value = context.IsNullable; return true;
            }

            value = false;
            return false;
        }
    }

    // Hand written rules. Registered before ColumnProvider, so they win.
    private sealed class ColumnProviderCustom : ContextProvider<Column>
    {
        public override bool TryToken(Column context, TokenArgs args, out string? value)
        {
            switch (args.Name)
            {
                // Uses the table around the column.
                case "full_name": value = args.FindParent<Table>()?.Name + "." + context.Name; return true;
            }

            value = null;
            return false;
        }

        public override bool TryCondition(Column context, ConditionArgs args, out bool value)
        {
            switch (args.Name)
            {
                // A key is never nullable, whatever the column says. The answer false still wins.
                case "is_nullable" when context.IsKey: value = false; return true;
            }

            value = false;
            return false;
        }
    }

    // Names that do not depend on the context.
    private sealed class ProjectGlobals : GlobalProvider
    {
        public override bool TryToken(TokenArgs args, out string? value)
        {
            switch (args.Name)
            {
                case "project_name": value = "Shop"; return true;
            }

            value = null;
            return false;
        }
    }

    public static void Run()
    {
        Console.WriteLine("== Typed providers ==");

        var registry = new ContextProviderRegistry()
            .Register(new TableProvider())
            .Register(new ColumnProviderCustom())
            .Register(new ColumnProvider())
            .RegisterGlobal(new ProjectGlobals(), GlobalOrder.BeforeTyped);

        var processor = new Processor();
        registry.AttachTo(processor);

        var table = new Table { Name = "Customer" };
        table.Columns.Add(new Column { Name = "Id", Type = "int", IsNullable = true, IsKey = true });
        table.Columns.Add(new Column { Name = "Email", Type = "string", IsNullable = true });

        const string template = "// <%=project_name%>\r\nclass <%=name%>\r\n<%FOREACH columns%>  <%=type%><%IF is_nullable%>?<%ENDIF%> <%=name%>; // <%=full_name%>\r\n<%ENDFOR%>";
        string text = processor.Process(new ProcessorArgs(new Parser().Parse(template)) { Context = table }).ToString();
        Console.WriteLine(text);

        Verify.Equal("// Shop\r\nclass Customer\r\n  int Id; // Customer.Id\r\n  string? Email; // Customer.Email\r\n", text, "typed providers");
    }
}
