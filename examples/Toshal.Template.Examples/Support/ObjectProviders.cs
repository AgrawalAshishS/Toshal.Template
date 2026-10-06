// Title: Helper: providers that read object properties
// Summary: The library never reads your objects by itself; providers decide what a name means. This helper builds a Processor whose providers read public properties by name, so plain C# objects can feed a template. The realistic examples use it.

using System.Collections;
using System.Globalization;
using System.Reflection;
using Toshal.Template.Tokens;

namespace Toshal.Template.Examples.Support;

public static class ObjectProviders
{
    /// <summary>
    /// A processor whose providers look a tag name up as a public property (not case sensitive): first on the current context,
    /// then on the parent contexts from the inside out. So inside a FOREACH of order lines, &lt;%=CustomerName%&gt; still finds the order.
    /// </summary>
    public static Processor Create(Func<ProcessTemplateArgs, List<IToken>?>? subTemplates = null)
    {
        return new Processor
        {
            // <%=Total format="0.00" pad="10"%>: the value, formatted with the optional format attribute,
            // and padded to a width (positive: right aligned, negative: left aligned) for text tables.
            TokenValueProvider = args => Pad(Format(Find(args.Context, args.ParentContext, args.Name), args.GetAttribute("format", "")), args.GetAttribute("pad", "0")),

            // <%IF HasDiscount%>: a bool property, or "has a value" for anything else.
            ConditionValueProvider = args => IsTrue(Find(args.Context, args.ParentContext, args.Name)),

            // <%FOREACH Lines%>: a property that is a list.
            LoopValueProvider = args => Find(args.Context, args.ParentContext, args.Name) as IList,

            // <%WITH Customer%>: a property that becomes the new context.
            WithValueProvider = args => Find(args.Context, args.ParentContext, args.Name),

            ProcessTemplateValueProvider = subTemplates,
        };
    }

    private static object? Find(object? context, List<object?> parents, string name)
    {
        // The current context first, then the parents from the inside out.
        var places = new List<object?> { context };
        for (int i = parents.Count - 1; i >= 0; i--) places.Add(parents[i]);

        foreach (var place in places)
        {
            if (place == null) continue;
            var property = place.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property != null && property.GetIndexParameters().Length == 0) return property.GetValue(place);
        }

        return null;
    }

    private static string? Format(object? value, string format)
    {
        if (value is IFormattable formattable && format.Length > 0) return formattable.ToString(format, CultureInfo.InvariantCulture);
        return Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    private static string? Pad(string? text, string width)
    {
        int w = int.Parse(width, CultureInfo.InvariantCulture);
        if (w == 0 || text == null) return text;
        return w > 0 ? text.PadLeft(w) : text.PadRight(-w);
    }

    private static bool IsTrue(object? value)
    {
        return value switch
        {
            null => false,
            bool b => b,
            string s => s.Length > 0,
            ICollection c => c.Count > 0,
            _ => true,
        };
    }
}
