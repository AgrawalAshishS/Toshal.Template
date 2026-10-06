// Run:  dotnet run -c Release --project benchmarks/Toshal.Template.Benchmarks -- --filter *
// The same template and data go through each provider style. Setup checks that every style writes the same text.

using BenchmarkDotNet.Attributes;
using Toshal.Template.Tokens;

namespace Toshal.Template.Benchmarks;

[MemoryDiagnoser]
public class ProcessBenchmarks
{
    private List<IToken> tokens = null!;
    private Node<T00> root = null!;
    private Processor chain = null!;
    private Processor registry = null!;
    private Processor floor = null!;

    [GlobalSetup]
    public void Setup()
    {
        this.tokens = new Parser().Parse(Data.Template);
        this.root = Data.Build();

        this.chain = new Processor
        {
            TokenValueProvider = ChainProvider.Token,
            ConditionValueProvider = ChainProvider.Condition,
            LoopValueProvider = ChainProvider.Loop,
            WithValueProvider = ChainProvider.With,
        };

        this.registry = new Processor();
        RegistrySetup.Build().AttachTo(this.registry);

        // The lower bound: a provider that knows the three types in use and nothing else. No real consumer can do this.
        this.floor = new Processor
        {
            TokenValueProvider = a => Globals.Token(a) ?? a.Context switch
            {
                Node<T29> => Custom<T29>.Token(a) ?? Generated<T29>.Token(a),
                Node<T15> => Generated<T15>.Token(a),
                _ => null,
            },
            ConditionValueProvider = a => Custom<T29>.Condition(a) || Generated<T29>.Condition(a),
            LoopValueProvider = a => a.Context is Node<T00> ? Generated<T00>.Loop(a) : Generated<T15>.Loop(a),
            WithValueProvider = Generated<T29>.With,
        };

        Expected = this.Chain();
        if (!Expected.Contains("public string? Column0 { get; set; } // Table0 nullable text", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The chain style wrote wrong text:\r\n" + Expected);
        }

        if (this.Registry() != Expected || this.Floor() != Expected)
        {
            throw new InvalidOperationException("The styles write different text.");
        }
    }

    public static string Expected { get; private set; } = "";

    [Benchmark(Baseline = true)]
    public string Chain() => this.chain.Process(new ProcessorArgs(this.tokens) { Context = this.root }).ToString();

    [Benchmark]
    public string Registry() => this.registry.Process(new ProcessorArgs(this.tokens) { Context = this.root }).ToString();

    [Benchmark]
    public string Floor() => this.floor.Process(new ProcessorArgs(this.tokens) { Context = this.root }).ToString();

    // The same, writing into one builder that is reused: no new builder, no growth, no ToString copy.
    private readonly System.Text.StringBuilder output = new();

    [Benchmark]
    public int RegistryIntoBuilder()
    {
        this.output.Clear();
        this.registry.Process(new ProcessorArgs(this.tokens) { Context = this.root }, this.output);
        return this.output.Length;
    }

    [Benchmark]
    public int FloorIntoBuilder()
    {
        this.output.Clear();
        this.floor.Process(new ProcessorArgs(this.tokens) { Context = this.root }, this.output);
        return this.output.Length;
    }
}
