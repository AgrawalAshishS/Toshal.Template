// Title: Compiled templates (.ctt)
// Summary: A .ctt file becomes a C# class at build time (Toshal.Template.Generator). No parser runs at run time; the values come from partial methods in the other half of the class, which is made once for you. The text is the same as the processor writes.

using Toshal.Template.Examples.CompiledTemplates;
using Toshal.Template.Examples.Support;

namespace Toshal.Template.Examples;

public static class CompiledTemplatesExample
{
    public static void Run()
    {
        Console.WriteLine("== Compiled templates ==");

        var customer = new CodeGenerationExample.TableModel("customer", "Shop.Data",
        [
            new CodeGenerationExample.Column("id", "integer", Nullable: false, Key: true, Comment: "The customer number."),
            new CodeGenerationExample.Column("full_name", "text", Nullable: false),
            new CodeGenerationExample.Column("email", "text", Nullable: true),
        ]);

        // CompiledTemplates/Entity.ctt is the same text as Templates/Entity.rtt. The class Entity was made from it when the project was built.
        // One instance can be used again and again, also from many threads.
        var entity = new Entity();
        string compiled = entity.Process(customer).ToString();
        Console.WriteLine(compiled);

        // The same template through the parser and the processor gives the same text.
        Verify.Equal(CodeGenerationExample.Generate("Entity.rtt", customer), compiled, "compiled template");
    }
}
