extern alias codegen;

using Toshal.Template.Compiled.Tests.Support;
using Toshal.Template.Exceptions;
using Toshal.Template.Tests.ParserGolden;

using Xunit;

using CSharpEmitter = codegen::Toshal.Template.CodeGen.CSharpEmitter;
using TemplateSource = codegen::Toshal.Template.CodeGen.TemplateSource;

namespace Toshal.Template.Compiled.Tests
{
    // Every tricky template of the parser golden file goes through Processor and through its compiled class, with lists of 0 to 5 rows,
    // with TokenValue and with WriteToken. The texts must be equal. A template the parser rejects must give the same error at build time.
    public class GoldenCasesTests
    {
        private static readonly int[] RowCounts = { 0, 1, 2, 3, 5 };

        private static readonly Lazy<CompiledHarness.Compiled> Built = new Lazy<CompiledHarness.Compiled>(() =>
        {
            var entries = new List<CompiledHarness.Entry>();
            for (int i = 0; i < ParserGoldenTests.Cases.Length; i++)
            {
                if (CoreError(ParserGoldenTests.Cases[i]) != null) continue;
                entries.Add(new CompiledHarness.Entry("C" + i, ParserGoldenTests.Cases[i]));
                entries.Add(new CompiledHarness.Entry("W" + i, ParserGoldenTests.Cases[i], Writer: true));
            }

            return CompiledHarness.Compile(entries);
        });

        public static IEnumerable<object[]> Indexes => Enumerable.Range(0, ParserGoldenTests.Cases.Length).Select(i => new object[] { i });

        [Theory]
        [MemberData(nameof(Indexes))]
        public void SameTextAsProcessor(int index)
        {
            var template = ParserGoldenTests.Cases[index];
            if (CoreError(template) != null) return;

            foreach (var rows in RowCounts)
            {
                foreach (var writer in new[] { false, true })
                {
                    var expected = CompiledHarness.RunProcessor(template, new TestProviders { RowCount = rows }, Top(), writer);
                    var actual = Built.Value.Run((writer ? "W" : "C") + index, new TestProviders { RowCount = rows }, Top(), writer);
                    if (expected != actual)
                    {
                        Assert.Fail($"rows {rows}, writer {writer}\r\nexpected: {expected}\r\nactual:   {actual}\r\n{Built.Value.Code["C" + index]}");
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(Indexes))]
        public void SameErrorAsParser(int index)
        {
            var template = ParserGoldenTests.Cases[index];
            var expected = CoreError(template);
            if (expected == null) return;

            var actual = Assert.ThrowsAny<Exception>(() => CSharpEmitter.Emit(new TemplateSource("N", "C", template)));
            Assert.Equal(expected.GetType().Name, actual.GetType().Name);
            Assert.Equal(expected.Message, actual.Message);
            Assert.Equal(expected.LineNumber, (int)actual.GetType().GetProperty("LineNumber")!.GetValue(actual)!);
            Assert.Equal(expected.StartingPosition, (int)actual.GetType().GetProperty("StartingPosition")!.GetValue(actual)!);
        }

        private static Dictionary<string, object?> Top() => new Dictionary<string, object?> { ["name"] = "top" };

        private static ParserException? CoreError(string template)
        {
            try
            {
                new Parser().Parse(template);
                return null;
            }
            catch (ParserException ex)
            {
                return ex;
            }
        }
    }
}
