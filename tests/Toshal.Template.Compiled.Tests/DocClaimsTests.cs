using System.Text;

using Toshal.Template.Compiled.Tests.Support;
using Toshal.Template.Tokens;

using Xunit;

namespace Toshal.Template.Compiled.Tests
{
    // Proves what the XML docs of Toshal.Template.Compiled say.
    public class DocClaimsTests
    {
        [Fact]
        public void AttributesMakesTheLowerCaseCopy()
        {
            var attributes = Probe.Make("format", "Upper Case", "pad", "5");

            Assert.Equal("Upper Case", attributes["format"]);
            Assert.Equal("upper case", attributes.LowerCaseValues["format"]);
            Assert.Equal("5", attributes.GetLowerCaseValue("PAD", "x"));
        }

        [Fact]
        public void AttributesNeedsPairsWithoutRepeats()
        {
            Assert.Throws<ArgumentNullException>(() => Probe.Make(null!));
            Assert.Throws<ArgumentException>(() => Probe.Make("a"));
            Assert.Throws<ArgumentException>(() => Probe.Make("a", "1", "a", "2"));
            Assert.Empty(Probe.Make());
        }

        [Fact]
        public void TheContextIsTheFirstParentOnlyWhenItIsNotNull()
        {
            var probe = new Probe();

            probe.Process("top");
            Assert.Equal(new object?[] { "top" }, probe.Parents);

            probe.Process(null);
            Assert.Empty(probe.Parents);
        }

        [Fact]
        public void OneInstanceCanBeUsedFromManyThreads()
        {
            var built = CompiledHarness.Compile(new[] { new CompiledHarness.Entry("Threads", FeatureCasesTests.Cases[14]) });
            var providers = new TestProviders();
            providers.CompiledSubTemplate = name => built.Create("Sub_" + name, providers);
            var template = built.Create("Threads", providers);
            var expected = template.Process(new Dictionary<string, object?> { ["name"] = "top" }).ToString();

            Parallel.For(0, 200, _ =>
            {
                var output = new StringBuilder();
                template.Process(new Dictionary<string, object?> { ["name"] = "top" }, output);
                Assert.Equal(expected, output.ToString());
            });
        }

        private sealed class Probe : CompiledTemplate
        {
            public List<object?> Parents { get; private set; } = new List<object?>();

            public static TokenAttributeDictionary Make(params string[] namesAndValues) => Attributes(namesAndValues);

            protected override void Render(StringBuilder output, object? context, TemplateRun run, ref TemplateScope scope)
            {
                this.Parents = new List<object?>(run.ParentContext);
            }
        }
    }
}
