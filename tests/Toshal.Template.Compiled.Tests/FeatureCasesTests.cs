using System.Text;

using Toshal.Template.Compiled.Tests.Support;

using Xunit;

namespace Toshal.Template.Compiled.Tests
{
    // Templates that use each feature with real data and its tricky corners: SET scopes, REMOVE_PREVIOUS, SEPARATOR, sub template indents,
    // nested FOREACH, REUSE_FOREACH, lines with only control tags. Each one must give the text Processor gives.
    public class FeatureCasesTests
    {
        public static readonly string[] Cases =
        {
            // 0: the example of the owner
            "The <%=Product abc=\"xyz\" %> team",

            // SET: a SET wins over the provider; inner blocks see it; a SET in a block with its own scope is not seen outside
            "<%SET name%>set<%ENDSET%><%=name%>",
            "<%IF a%><%SET v%>in if<%ENDSET%><%ENDIF%>[<%=v%>]",
            "<%WITH w%><%SET v%>in with<%ENDSET%><%=v%><%ENDWITH%>[<%=v%>]",
            "<%FOREACH rows%><%=v%><%SET v%><%=name%><%ENDSET%>,<%ENDFOR%>[<%=v%>]",
            "<%FOREACH rows%><%BEFOREROW%><%SET b%>B<%ENDSET%><%ENDBEFOREROW%><%=b%><%ROW%><%=b%>r<%SET r%>R<%ENDSET%><%ENDROW%><%AFTERROW%><%=r%><%ENDAFTERROW%><%ENDFOR%>",
            "<%SET outer%>o<%ENDSET%><%SET v%>a<%SET v%>b<%ENDSET%><%=v%><%ENDSET%><%=v%><%=outer%>",
            "<%SET v%>x<%REMOVE_PREVIOUS 5%>y<%ENDSET%><%=v%>",
            "<%SET x%>outer<%ENDSET%><%PROCESS_TEMPLATE footer%>[<%=x%>]",

            // REMOVE_PREVIOUS
            "a,b,<%REMOVE_PREVIOUS 1%>",
            "ab<%REMOVE_PREVIOUS 9%>c",
            "line\r\n<%REMOVE_PREVIOUS_NEW_LINE%>end",
            "line\n<%REMOVE_PREVIOUS_NEW_LINE%><%REMOVE_PREVIOUS_NEW_LINE%>end",
            "<%FOREACH rows%><%=name%>, <%ENDFOR%><%REMOVE_PREVIOUS 2%>",
            "<%=name%>xy<%REMOVE_PREVIOUS 5%>|ab<%REMOVE_PREVIOUS 0%>",
            "<%SET v%>x\r<%ENDSET%><%=v%>\n<%REMOVE_PREVIOUS_NEW_LINE%>|<%=v%><%REMOVE_PREVIOUS_NEW_LINE%>|",
            "a\r\n\r\n<%REMOVE_PREVIOUS_NEW_LINE%><%REMOVE_PREVIOUS_NEW_LINE%><%REMOVE_PREVIOUS_NEW_LINE%>b\r<%REMOVE_PREVIOUS_NEW_LINE%>c",
            "\n<%REMOVE_PREVIOUS_NEW_LINE%>start<%IF a%>x\r\n<%ENDIF%><%REMOVE_PREVIOUS_NEW_LINE%>end",

            // FOREACH parts
            "<%FOREACH rows%><%HEADER%>[<%CONTEXT_AS_STRING%>]<%ENDHEADER%><%FIRSTROW%>F<%=name%><%ENDFIRSTROW%><%ROW%>R<%=name%><%ENDROW%><%ALTROW%>A<%=name%><%ENDALTROW%><%LASTROW%>L<%=name%><%ENDLASTROW%><%FOOTER%>(<%=name%>)<%ENDFOOTER%><%NORECORD%>none<%ENDNORECORD%><%ENDFOR%>",
            "<%FOREACH rows%><%BEFOREFIRSTROW%>bf<%ENDBEFOREFIRSTROW%><%BEFOREALTROW%>ba<%ENDBEFOREALTROW%><%BEFORELASTROW%>bl<%ENDBEFORELASTROW%><%=name%><%AFTERFIRSTROW%>af<%ENDAFTERFIRSTROW%><%AFTERALTROW%>aa<%ENDAFTERALTROW%><%AFTERLASTROW%>al<%ENDAFTERLASTROW%>;<%ENDFOR%>",
            "<%FOREACH rows%><%LASTROW%>last<%ENDLASTROW%><%FIRSTROW%>first<%ENDFIRSTROW%><%ENDFOR%>",
            "<%FOREACH rows%><%NORECORD%><%SEPARATOR%>never<%ENDSEPARATOR%>empty<%ENDNORECORD%><%ENDFOR%>",
            "<%FOREACH rows%><%FOREACH inner%><%=name%><%SEPARATOR%>+<%ENDSEPARATOR%><%ENDFOR%><%SEPARATOR%> | <%ENDSEPARATOR%><%ENDFOR%>",
            "<%FOREACH rows%><%FOREACH inner%>i<%ENDFOR%><%SEPARATOR%>,<%ENDSEPARATOR%><%ENDFOR%>",
            "<%FOREACH rows%><%=name%><%ENDFOR%>|<%REUSE_FOREACH rows again%>|<%WITH w%><%REUSE_FOREACH rows more%><%ENDWITH%>",
            "<%FOREACH rows top=\"2\"%><%=name%><%ENDFOR%><%REUSE_FOREACH rows again%>",
            "<%FOREACH rows%><%IF not odd%><%CONTEXT_AS_STRING%><%ELSEIF even x=\"1\"%>e<%ELSE%>o<%ENDIF%><%ENDFOR%>",

            // Lines with only control tags are dropped, and a sub template alone on its line is indented
            "class C\r\n{\r\n    <%FOREACH rows%>\r\n    int <%=name%>;\r\n    <%ENDFOR%>\r\n}\r\n",
            "begin\r\n    <%PROCESS_TEMPLATE footer%>\r\nend",
            "begin\n\t<%PROCESS_TEMPLATE footer%>\n\t<%PROCESS_TEMPLATE missing%>\nend",
            "<%FOREACH rows%>\r\n  <%PROCESS_TEMPLATE footer%>\r\n<%ENDFOR%>",

            // WITH, parent stack and the context as text
            "<%WITH w%><%WITH missing%>never<%ENDWITH%><%WITH x%><%=name%><%ENDWITH%><%ENDWITH%>",
            "<%CONTEXT_AS_STRING%>",
            "<%=empty%>|<%=null%>|<%=a q=\"Mixed Case\" r=\"x\"%>",
            "Quotes \" and \\ and \t tab\r\n<%=x%>",
        };

        private static readonly Lazy<CompiledHarness.Compiled> Built = new Lazy<CompiledHarness.Compiled>(() =>
            CompiledHarness.Compile(Cases.SelectMany((template, i) => new[]
            {
                new CompiledHarness.Entry("C" + i, template),
                new CompiledHarness.Entry("W" + i, template, Writer: true),
            })));

        public static IEnumerable<object[]> Indexes => Enumerable.Range(0, Cases.Length).Select(i => new object[] { i });

        [Theory]
        [MemberData(nameof(Indexes))]
        public void SameTextAsProcessor(int index)
        {
            foreach (var rows in new[] { 0, 1, 2, 3, 4 })
            {
                foreach (var writer in new[] { false, true })
                {
                    var expected = CompiledHarness.RunProcessor(Cases[index], new TestProviders { RowCount = rows }, Top(), writer);
                    var actual = Built.Value.Run((writer ? "W" : "C") + index, new TestProviders { RowCount = rows }, Top(), writer);
                    if (expected != actual)
                    {
                        Assert.Fail($"rows {rows}, writer {writer}\r\nexpected: {expected}\r\nactual:   {actual}\r\n{Built.Value.Code["C" + index]}");
                    }
                }
            }
        }

        [Fact]
        public void TheExampleOfTheOwnerWritesTheValue()
        {
            var text = Built.Value.Run("C0", new TestProviders(), new Dictionary<string, object?> { ["product"] = "Toshal" });

            Assert.Equal("The Toshal team", text);
        }

        [Fact]
        public void TheProvidersSeeTheArgsProcessorGives()
        {
            // name, attribute with its lower case copy, size of the parent stack, context
            Assert.Equal("The {product abc=xyz/xyz @1 ctx:top} team", Built.Value.Run("C0", new TestProviders(), Top()));
        }

        [Fact]
        public void TextAlreadyInTheOutputStaysAndIsNotRemoved()
        {
            int index = Array.IndexOf(Cases, "ab<%REMOVE_PREVIOUS 9%>c");
            var providers = new TestProviders();
            providers.CompiledSubTemplate = name => Built.Value.Create("Sub_" + name, providers);

            var output = new StringBuilder("keep:");
            Built.Value.Create("C" + index, providers).Process(null, output);

            Assert.Equal("keep:c", output.ToString());
        }

        [Fact]
        public void OneInstanceRunsManyTimesWithTheSameText()
        {
            var providers = new TestProviders();
            providers.CompiledSubTemplate = name => Built.Value.Create("Sub_" + name, providers);
            var template = Built.Value.Create("C14", providers);

            var first = template.Process(Top()).ToString();
            var output = new StringBuilder();
            for (int i = 0; i < 3; i++)
            {
                output.Clear();
                template.Process(Top(), output);
                Assert.Equal(first, output.ToString());
            }
        }

        [Fact]
        public void ANullOutputThrows()
        {
            var template = Built.Value.Create("C0", new TestProviders());

            Assert.Throws<ArgumentNullException>(() => template.Process(null, null!));
        }

        private static Dictionary<string, object?> Top() => new Dictionary<string, object?> { ["name"] = "top" };
    }
}
