// Processing cost of SET inside loops and of record contexts (records compare all their fields in Equals).

using System.Collections;
using BenchmarkDotNet.Attributes;
using Toshal.Template.Tokens;

namespace Toshal.Template.Benchmarks;

public sealed record Person(string Name, string City, int Age, string Email, string Phone);

public sealed record Team(string Name, List<Person> People);

[MemoryDiagnoser]
public class FeatureBenchmarks
{
    private const string Template =
        "<%FOREACH teams%><%=name%>:\r\n" +
        "<%FOREACH people%><%SET label%><%=name%> (<%=city%>)<%ENDSET%>  <%=label%><%WITH self%> <%=email%><%ENDWITH%>\r\n<%ENDFOR%>" +
        "<%ENDFOR%>";

    private List<IToken> tokens = null!;
    private Processor processor = null!;
    private List<Team> teams = null!;

    [GlobalSetup]
    public void Setup()
    {
        this.tokens = new Parser().Parse(Template);
        this.teams = Enumerable.Range(0, 10)
            .Select(t => new Team("Team" + t, Enumerable.Range(0, 20).Select(p => new Person("P" + p, "City" + p, p, "p" + p + "@x.com", "555-" + p)).ToList()))
            .ToList();

        this.processor = new Processor
        {
            LoopValueProvider = a => a.Context switch
            {
                List<Team> list => list,
                Team team => team.People,
                _ => null,
            },
            WithValueProvider = a => a.Context,
            TokenValueProvider = a => a.Context switch
            {
                Team team => team.Name,
                Person p => a.Name switch { "name" => p.Name, "city" => p.City, "email" => p.Email, _ => null },
                _ => null,
            },
        };

        if (!this.SetAndRecords().Contains("  P1 (City1) p1@x.com", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Wrong text: " + this.SetAndRecords());
        }
    }

    [Benchmark]
    public string SetAndRecords() => this.processor.Process(new ProcessorArgs(this.tokens) { Context = this.teams }).ToString();
}
