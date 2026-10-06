// Parsing cost: a small code generation template and a bigger one with every kind of tag, repeated.

using BenchmarkDotNet.Attributes;

namespace Toshal.Template.Benchmarks;

[MemoryDiagnoser]
public class ParseBenchmarks
{
    private const string Block =
        "<%SET title%>Report <%=name format=\"upper\"%><%ENDSET%>\r\n" +
        "<%IF has_rows%><%=title%>\r\n<%ELSEIF not empty%>none\r\n<%ELSE%>?<%ENDIF%>\r\n" +
        "<%FOREACH rows%><%HEADER%>Header <%=count%>\r\n<%ENDHEADER%><%ROW%>  <%=name%>: <%=value format=\"0.00\"%>\r\n<%ENDROW%>" +
        "<%ALTROW%>  * <%=name%>\r\n<%ENDALTROW%><%NORECORD%>no rows<%ENDNORECORD%><%FOOTER%>end<%REMOVE_PREVIOUS_NEW_LINE%><%ENDFOOTER%><%ENDFOR%>\r\n" +
        "<%WITH customer%><%=name%>, <%CONTEXT_AS_STRING%><%ENDWITH%><%REMOVE_PREVIOUS 2%>\r\n" +
        "Plain text line with no tags at all, long enough to look like a real template line.\r\n";

    private string big = "";

    [GlobalSetup]
    public void Setup()
    {
        // Each copy goes inside its own WITH, so the FOREACH names stay unique per level.
        var text = new System.Text.StringBuilder();
        for (var i = 0; i < 50; i++)
        {
            text.Append("<%WITH part").Append(i).Append("%>").Append(Block).Append("<%ENDWITH%>\r\n");
        }

        this.big = text.ToString();
    }

    [Benchmark]
    public int CodeGeneration() => new Parser().Parse(Data.Template).Count;

    [Benchmark]
    public int AllTags50() => new Parser().Parse(this.big).Count;
}
