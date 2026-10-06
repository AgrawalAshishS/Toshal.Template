using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Toshal.Template.Providers;
using Toshal.Template.Tokens;
using Xunit;

namespace Toshal.Template.Tests
{
    // Each test proves one sentence of the XML docs or the README. If a test here fails, fix the docs, not the test.
    public class DocClaimsTests
    {
        private static string Run(string template, Processor processor, object? context = null)
        {
            return processor.Process(new ProcessorArgs(new Parser().Parse(template)) { Context = context }).ToString();
        }

        private static Processor ListOf(params object[] rows)
        {
            return new Processor { LoopValueProvider = args => rows.ToList() };
        }

        [Fact]
        public void ReadmeExample()
        {
            var tokens = new Parser().Parse("Hello <%=Name%>!");
            var processor = new Processor { TokenValueProvider = args => args.Name == "name" ? "World" : null };

            Assert.Equal("Hello World!", processor.Process(new ProcessorArgs(tokens)).ToString());
        }

        [Fact]
        public void ContextAsStringWritesEachItem()
        {
            Assert.Equal("#a #b ", Run("<%FOREACH tags%>#<%CONTEXT_AS_STRING%> <%ENDFOR%>", ListOf("a", "b")));
        }

        [Fact]
        public void RemovePreviousDropsTheLastSeparator()
        {
            Assert.Equal("a, b", Run("<%FOREACH tags%><%CONTEXT_AS_STRING%>, <%ENDFOR%><%REMOVE_PREVIOUS 2%>", ListOf("a", "b")));
        }

        [Fact]
        public void RemovePreviousRemovesEverythingWhenFewerCharactersWereWritten()
        {
            Assert.Equal("", Run("ab<%REMOVE_PREVIOUS 5%>", new Processor()));
        }

        [Fact]
        public void RemovePreviousNewLineRemovesCrLf()
        {
            Assert.Equal("HelloWorld", Run("Hello\r\n<%REMOVE_PREVIOUS_NEW_LINE%>World", new Processor()));
        }

        [Fact]
        public void RemovePreviousNewLineRemovesALoneCr()
        {
            Assert.Equal("Hello", Run("Hello\r<%REMOVE_PREVIOUS_NEW_LINE%>", new Processor()));
        }

        [Fact]
        public void MissingConditionProviderSkipsTheWholeIfIncludingElse()
        {
            Assert.Equal("[]", Run("[<%IF a%>x<%ELSE%>y<%ENDIF%>]", new Processor()));
        }

        [Fact]
        public void MissingLoopProviderSkipsTheWholeLoopIncludingNoRecord()
        {
            Assert.Equal("[]", Run("[<%FOREACH a%>x<%NORECORD%>none<%ENDNORECORD%><%ENDFOR%>]", new Processor()));
        }

        [Fact]
        public void MissingTokenProviderStillWritesSetVariables()
        {
            Assert.Equal("v", Run("<%SET x%>v<%ENDSET%><%=x%>", new Processor()));
        }

        [Fact]
        public void SetVariableWinsOverTheTokenProvider()
        {
            var processor = new Processor { TokenValueProvider = args => "provider" };
            Assert.Equal("set", Run("<%SET x%>set<%ENDSET%><%=x%>", processor));
        }

        [Fact]
        public void NullOrEmptyTokenValueWritesNothing()
        {
            var processor = new Processor { TokenValueProvider = args => args.Name == "a" ? null : "" };
            Assert.Equal("[]", Run("[<%=a%><%=b%>]", processor));
        }

        [Fact]
        public void NotIsAppliedByTheProcessor()
        {
            string? seenName = null;
            var processor = new Processor { ConditionValueProvider = args => { seenName = args.Name; return true; } };

            Assert.Equal("no", Run("<%IF not Paid%>yes<%ELSE%>no<%ENDIF%>", processor));
            Assert.Equal("paid", seenName);
        }

        [Fact]
        public void ThenIsOptional()
        {
            var a = (ConditionToken)new Parser().Parse("<%IF Paid THEN%>x<%ENDIF%>")[0];
            var b = (ConditionToken)new Parser().Parse("<%IF Paid%>x<%ENDIF%>")[0];
            Assert.Equal(a.Name, b.Name);
        }

        [Fact]
        public void NamesAreLowerCasedAndAttributeValuesKeptForValueTags()
        {
            var token = (NamedToken)new Parser().Parse("<%=FirstName Format=\"Upper\"%>")[0];

            Assert.Equal("firstname", token.Name);
            Assert.Equal("Upper", token.GetAttribute("FORMAT", "x"));
            Assert.Equal("x", token.GetAttribute("missing", "x"));
            Assert.Throws<KeyNotFoundException>(() => token.Attributes["Format"]);
        }

        [Fact]
        public void OnlyTheParserFillsLowerCaseValues()
        {
            var attributes = new TokenAttributeDictionary { ["format"] = "N2" };

            Assert.Empty(attributes.LowerCaseValues);
            Assert.Equal("x", attributes.GetLowerCaseValue("format", "x"));
        }

        [Fact]
        public void ArgsGetAttributeReadsTheTagAttributes()
        {
            string? seen = null;
            var processor = new Processor { TokenValueProvider = args => { seen = args.GetAttribute("Format", "none"); return null; } };
            Run("<%=Total format=\"0.00\"%>", processor);
            Assert.Equal("0.00", seen);

            string? lang = null;
            var templates = new Processor { ProcessTemplateValueProvider = args => { lang = args.GetAttribute("lang", "en"); return null; } };
            Run("<%PROCESS_TEMPLATE footer lang=\"fr\"%>", templates);
            Assert.Equal("fr", lang);
        }

        [Fact]
        public void HeaderAndFooterSeeTheListAndAreSkippedForAnEmptyList()
        {
            var processor = new Processor
            {
                LoopValueProvider = args => args.Name == "full" ? new List<int> { 1, 2 } : new List<int>(),
                TokenValueProvider = args => args.Context is IList list ? list.Count.ToString() : "?",
            };

            Assert.Equal("h2 f2", Run("<%FOREACH full%><%HEADER%>h<%=n%> <%ENDHEADER%><%FOOTER%>f<%=n%><%ENDFOOTER%><%ENDFOR%>", processor));
            Assert.Equal("none", Run("<%FOREACH empty%><%HEADER%>h<%ENDHEADER%><%NORECORD%>none<%ENDNORECORD%><%ENDFOR%>", processor));
        }

        [Fact]
        public void NullListRunsNoRecord()
        {
            var processor = new Processor { LoopValueProvider = args => null };
            Assert.Equal("none", Run("<%FOREACH a%>x<%NORECORD%>none<%ENDNORECORD%><%ENDFOR%>", processor));
        }

        [Fact]
        public void TextOutsideEveryPartBelongsToRow()
        {
            Assert.Equal("xyzxyz", Run("<%FOREACH a%>x<%ROW%>y<%ENDROW%>z<%ENDFOR%>", ListOf(1, 2)));
        }

        [Fact]
        public void OddLastRowWithoutLastRowUsesAltRow()
        {
            Assert.Equal("FAR", Run("<%FOREACH a%><%ROW%>R<%ENDROW%><%ALTROW%>A<%ENDALTROW%><%FIRSTROW%>F<%ENDFIRSTROW%><%ENDFOR%>", ListOf(1, 2, 3)));
            Assert.Equal("FA", Run("<%FOREACH a%><%ROW%>R<%ENDROW%><%ALTROW%>A<%ENDALTROW%><%FIRSTROW%>F<%ENDFIRSTROW%><%ENDFOR%>", ListOf(1, 2)));
        }

        [Fact]
        public void ReuseForEachCanComeBeforeTheForEach()
        {
            var processor = new Processor { LoopValueProvider = args => args.Name == "done" ? new List<int> { 1 } : new List<int> { 1, 2 } };
            Assert.Equal("x|xx", Run("<%REUSE_FOREACH open done%>|<%FOREACH open%>x<%ENDFOR%>", processor));
        }

        [Fact]
        public void ParentContextHoldsTopContextListAndItem()
        {
            var rows = new List<string> { "a" };
            List<object?>? seen = null;
            var processor = new Processor
            {
                LoopValueProvider = args => rows,
                TokenValueProvider = args => { seen = args.ParentContext.ToList(); return null; },
            };

            Run("<%FOREACH l%><%=t%><%ENDFOR%>", processor, context: "root");

            Assert.Equal(new object?[] { "root", rows, "a" }, seen);
        }

        [Fact]
        public void SetScopes()
        {
            var processor = new Processor
            {
                WithValueProvider = args => "w",
                ConditionValueProvider = args => true,
                LoopValueProvider = args => new List<int> { 1, 2 },
            };

            // inside WITH: stays inside
            Assert.Equal("[]", Run("<%WITH a%><%SET x%>v<%ENDSET%><%ENDWITH%>[<%=x%>]", processor));
            // inside IF: visible after
            Assert.Equal("[v]", Run("<%IF a%><%SET x%>v<%ENDSET%><%ENDIF%>[<%=x%>]", processor));
            // inside ROW: visible to BEFOREROW of the next row, not after the loop
            Assert.Equal("()r(v)r[]", Run("<%FOREACH a%><%BEFOREROW%>(<%=x%>)<%ENDBEFOREROW%><%ROW%>r<%SET x%>v<%ENDSET%><%ENDROW%><%ENDFOR%>[<%=x%>]", processor));
        }

        [Fact]
        public void ProcessTemplateSeesCallerVariablesAndKeepsItsOwn()
        {
            var parser = new Parser();
            var processor = new Processor { ProcessTemplateValueProvider = args => parser.Parse("<%=x%><%SET y%>inner<%ENDSET%>") };

            Assert.Equal("outer[]", Run("<%SET x%>outer<%ENDSET%><%PROCESS_TEMPLATE sub%>[<%=y%>]", processor));
        }

        [Fact]
        public void WithNullSkipsTheBlock()
        {
            var processor = new Processor { WithValueProvider = args => null };
            Assert.Equal("[]", Run("[<%WITH a%>x<%ENDWITH%>]", processor));
        }

        [Fact]
        public void EmptyTemplateGivesEmptyList()
        {
            Assert.Empty(new Parser().Parse(""));
        }

        [Fact]
        public void TokensCanBeProcessedManyTimes()
        {
            var tokens = new Parser().Parse("<%=a%>");
            var processor = new Processor { TokenValueProvider = args => (string?)args.Context };

            Assert.Equal("1", processor.Process(new ProcessorArgs(tokens) { Context = "1" }).ToString());
            Assert.Equal("2", processor.Process(new ProcessorArgs(tokens) { Context = "2" }).ToString());
        }

        [Fact]
        public void SignatureInterfacesMatchTheProcessorProperties()
        {
            var provider = new Provider();
            var processor = new Processor
            {
                TokenValueProvider = provider.TokenValueProvider,
                ConditionValueProvider = provider.ConditionValueProvider,
                LoopValueProvider = provider.LoopValueProvider,
                WithValueProvider = provider.WithValueProvider,
                ProcessTemplateValueProvider = provider.ProcessTemplateValueProvider,
            };

            Assert.Equal("t|c|l|w|p", Run("<%=a%>|<%IF a%>c<%ENDIF%>|<%FOREACH a%>l<%ENDFOR%>|<%WITH a%>w<%ENDWITH%>|<%PROCESS_TEMPLATE a%>", processor));
        }

        [Fact]
        public void ProviderMethodsThatAreNotOverriddenAnswerNotHandled()
        {
            var typed = new EmptyTyped();
            var global = new EmptyGlobal();
            var token = (NamedToken)new Parser().Parse("<%=a%>")[0];
            var args = new TokenArgs(token, "x", new List<object?>());
            var condition = new ConditionArgs((ConditionToken)new Parser().Parse("<%IF a%><%ENDIF%>")[0], "x", new List<object?>());
            var loop = new LoopArgs("a", "x", new List<object?>(), new TokenAttributeDictionary());

            Assert.False(typed.TryToken("x", args, out _));
            Assert.False(typed.TryCondition("x", condition, out _));
            Assert.False(typed.TryLoop("x", loop, out _));
            Assert.False(typed.TryWith("x", args, out _));
            Assert.False(global.TryToken(args, out _));
            Assert.False(global.TryCondition(condition, out _));
            Assert.False(global.TryLoop(loop, out _));
            Assert.False(global.TryWith(args, out _));
        }

        [Fact]
        public void RegistryLookupsAllocateNothingAfterTheFirstLookupOfAType()
        {
            var registry = new ContextProviderRegistry().Register(new NameOfString());
            var token = (NamedToken)new Parser().Parse("<%=name%>")[0];
            var args = new TokenArgs(token, "x", new List<object?> { "x" });
            registry.Token(args);

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 1000; i++)
            {
                registry.Token(args);
                registry.Condition(new ConditionArgs((ConditionToken)ConditionTokenInstance, "x", args.ParentContext));
            }

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            // Only the 1000 ConditionArgs made by this test allocate.
            Assert.True(allocated <= 1000 * 64, $"Allocated {allocated} bytes.");
        }

        private static readonly IToken ConditionTokenInstance = new Parser().Parse("<%IF a%><%ENDIF%>")[0];

        [Fact]
        public void GlobalProvidersComeAfterTypedProvidersByDefault()
        {
            var registry = new ContextProviderRegistry().Register(new NameOfString()).RegisterGlobal(new NameGlobal());

            Assert.Equal("typed", Run("<%=name%>", AttachTo(registry), "x"));
            Assert.Equal("global", Run("<%=name%>", AttachTo(registry), null));
        }

        private static Processor AttachTo(ContextProviderRegistry registry)
        {
            var processor = new Processor();
            registry.AttachTo(processor);
            return processor;
        }

        private sealed class EmptyTyped : ContextProvider<string> { }

        private sealed class EmptyGlobal : GlobalProvider { }

        private sealed class NameOfString : ContextProvider<string>
        {
            public override bool TryToken(string context, TokenArgs args, out string? value)
            {
                value = "typed";
                return args.Name == "name";
            }
        }

        private sealed class NameGlobal : GlobalProvider
        {
            public override bool TryToken(TokenArgs args, out string? value)
            {
                value = "global";
                return args.Name == "name";
            }
        }

        private sealed class Provider : Signatures.ITokenValueProvider, Signatures.IConditionValueProvider, Signatures.ILoopValueProvider,
            Signatures.IWithValueProvider, Signatures.IProcessTemplateValueProvider
        {
            public string TokenValueProvider(TokenArgs args) => "t";

            public bool ConditionValueProvider(ConditionArgs args) => true;

            public IList LoopValueProvider(LoopArgs args) => new[] { 1 };

            public object WithValueProvider(TokenArgs args) => new object();

            public List<IToken> ProcessTemplateValueProvider(ProcessTemplateArgs args) => new Parser().Parse("p");
        }
    }
}
