// The data and the providers of the benchmarks. Node<TTag> gives 30 different context types with one shape.

using System.Collections;

namespace Toshal.Template.Benchmarks;

public sealed class Node<TTag> where TTag : struct
{
    public string Name { get; set; } = "";

    public string TypeName { get; set; } = "";

    public bool IsNullable { get; set; }

    public List<object> Items { get; } = new();

    public object? Parent { get; set; }
}

// The hand written providers: names that the generated ones do not know.
public static class Custom<TTag> where TTag : struct
{
    public static string? Token(TokenArgs a)
    {
        if (a.Context is not Node<TTag> n) return null;
        switch (a.Name)
        {
            case "nullable_type_name": return n.IsNullable ? n.TypeName + "?" : n.TypeName;
        }

        return null;
    }

    public static bool Condition(ConditionArgs a)
    {
        if (a.Context is not Node<TTag> n) return false;
        switch (a.Name)
        {
            case "is_string": return n.TypeName == "string";
        }

        return false;
    }

    public static IList? Loop(LoopArgs a) => null;

    public static object? With(TokenArgs a) => null;
}

// The generated providers: one case per property, with the alias written twice like the real ones.
public static class Generated<TTag> where TTag : struct
{
    public static string? Token(TokenArgs a)
    {
        if (a.Context is not Node<TTag> n) return null;
        switch (a.Name)
        {
            case "name": return n.Name;
            case "typename":
            case "type_name": return n.TypeName;
            case "isnullable":
            case "is_nullable": return n.IsNullable.ToString();
        }

        return null;
    }

    public static bool Condition(ConditionArgs a)
    {
        if (a.Context is not Node<TTag> n) return false;
        switch (a.Name)
        {
            case "name": return n.Name.Length > 0;
            case "isnullable":
            case "is_nullable": return n.IsNullable;
            case "items": return n.Items.Count > 0;
        }

        return false;
    }

    public static IList? Loop(LoopArgs a)
    {
        if (a.Context is not Node<TTag> n) return null;
        switch (a.Name)
        {
            case "items": return n.Items;
        }

        return null;
    }

    public static object? With(TokenArgs a)
    {
        if (a.Context is not Node<TTag> n) return null;
        switch (a.Name)
        {
            case "parent": return n.Parent;
        }

        return null;
    }
}

// Names that do not depend on the context.
public static class Globals
{
    public static string? Token(TokenArgs a)
    {
        switch (a.Name)
        {
            case "project_name": return "Shop";
            case "company_name": return "Toshal";
            case "current_year": return "2026";
        }

        return null;
    }
}

public static class Data
{
    // The template of a code generator: classes from tables, properties from columns.
    public const string Template =
        "// <%=project_name%> by <%=company_name%>\r\n" +
        "<%FOREACH items%>public class <%=name%>\r\n{\r\n" +
        "<%FOREACH items%>    public <%=nullable_type_name%> <%=name%> { get; set; } // <%WITH parent%><%=name%><%ENDWITH%><%IF is_nullable%> nullable<%ENDIF%><%IF is_string%> text<%ENDIF%>\r\n<%ENDFOR%>" +
        "}\r\n<%ENDFOR%>";

    // Project (T00) -> 10 tables (T15) -> 20 columns each (T29, the last type in the chain).
    public static Node<T00> Build()
    {
        var root = new Node<T00> { Name = "Shop" };
        for (var t = 0; t < 10; t++)
        {
            var table = new Node<T15> { Name = "Table" + t, Parent = root };
            for (var c = 0; c < 20; c++)
            {
                table.Items.Add(new Node<T29> { Name = "Column" + c, TypeName = c % 3 == 0 ? "string" : "int", IsNullable = c % 2 == 0, Parent = table });
            }

            root.Items.Add(table);
        }

        return root;
    }
}
