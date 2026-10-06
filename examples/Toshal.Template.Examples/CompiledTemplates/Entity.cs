// Title: Compiled template: the values of Entity.ctt
// Summary: The other half of the class made from CompiledTemplates/Entity.ctt. The build made it once with a case for every name; here it is filled in with typed code.

// Made once by Toshal.Template from Entity.ctt. This file is yours: give the values here.
// It is not made again. When the template uses a new kind of tag, the compiler names the method to add.

#nullable enable

namespace Toshal.Template.Examples.CompiledTemplates
{
    using System.Collections;

    using Toshal.Template;

    using static Toshal.Template.Examples.CodeGenerationExample;

    // The values of Entity.ctt, read straight from the typed objects: no reflection and no lookup by name at run time.
    public partial class Entity
    {
        // The text of <%=name%>. Null or empty writes nothing.
        private partial string? TokenValue(TokenArgs args) => args.Context switch
        {
            TableModel table => args.Name switch
            {
                "classname" => table.ClassName,
                "namespace" => table.Namespace,
                "table" => table.Table,
                _ => null,
            },
            Column column => args.Name switch
            {
                "comment" => column.Comment,
                "cstype" => column.CsType,
                "propertyname" => column.PropertyName,
                _ => null,
            },
            _ => null,
        };

        // Whether <%IF name%> or <%ELSEIF name%> is true. For <%IF not name%> you get the name without not.
        private partial bool Condition(ConditionArgs args) => args.Context is Column column && args.Name switch
        {
            "hascomment" => column.HasComment,
            "isstring" => column.IsString,
            _ => false,
        };

        // The rows of <%FOREACH name%>. Null or an empty list writes the NORECORD part.
        private partial IList? Loop(LoopArgs args) => args.Context is TableModel table && args.Name == "columns" ? table.Columns : null;
    }
}
