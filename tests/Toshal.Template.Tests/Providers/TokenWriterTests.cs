using System;
using System.Collections.Generic;
using System.Text;

using Toshal.Template.Providers;
using Xunit;

namespace Toshal.Template.Tests.Providers
{
    public class TokenWriterTests
    {
        private sealed class Item
        {
            public int Count { get; set; }

            public string Name { get; set; } = "";
        }

        // Writes numbers straight into the output; answers the name through TryToken only.
        private sealed class ItemProvider : ContextProvider<Item>
        {
            public override bool TryToken(Item context, TokenArgs args, out string? value)
            {
                switch (args.Name)
                {
                    case "name": value = context.Name; return true;
                }

                value = null;
                return false;
            }

            public override bool TryWrite(Item context, TokenArgs args, StringBuilder output)
            {
                switch (args.Name)
                {
                    case "count": output.Append(context.Count); return true;
                    case "label": output.Append(context.Name).Append(" x").Append(context.Count); return true;
                }

                return base.TryWrite(context, args, output);
            }
        }

        private sealed class YearGlobal : GlobalProvider
        {
            public override bool TryWrite(TokenArgs args, StringBuilder output)
            {
                if (args.Name != "year") return false;
                output.Append(2026);
                return true;
            }
        }

        private sealed class NameGlobal : GlobalProvider
        {
            public override bool TryToken(TokenArgs args, out string? value)
            {
                value = "global";
                return args.Name == "gname";
            }
        }

        private static string Run(ContextProviderRegistry registry, string template, object? context)
        {
            var processor = new Processor();
            registry.AttachTo(processor);
            return processor.Process(new ProcessorArgs(new Parser().Parse(template)) { Context = context }).ToString();
        }

        [Fact]
        public void TryWriteAppendsStraightToTheOutput()
        {
            var registry = new ContextProviderRegistry().Register(new ItemProvider());

            Assert.Equal("Pen: 3 (Pen x3)", Run(registry, "<%=name%>: <%=count%> (<%=label%>)", new Item { Name = "Pen", Count = 3 }));
        }

        [Fact]
        public void DefaultTryWriteUsesTryToken()
        {
            var registry = new ContextProviderRegistry()
                .Register(new ItemProvider())
                .RegisterGlobal(new NameGlobal())
                .RegisterGlobal(new YearGlobal(), GlobalOrder.BeforeTyped);

            Assert.Equal("Pen global 2026", Run(registry, "<%=name%> <%=gname%> <%=year%>", new Item { Name = "Pen" }));
        }

        [Fact]
        public void TokenAnswersWithTheSameTextAsWrite()
        {
            var registry = new ContextProviderRegistry().Register(new ItemProvider()).RegisterGlobal(new YearGlobal());
            var item = new Item { Name = "Pen", Count = 3 };
            var parent = new List<object?> { item };

            foreach (var name in new[] { "name", "count", "label", "year", "none" })
            {
                var args = new TokenArgs((Tokens.NamedToken)new Parser().Parse("<%=" + name + "%>")[0], item, parent);
                var output = new StringBuilder();
                registry.Write(args, output);
                Assert.Equal(output.ToString(), registry.Token(args) ?? "");
            }
        }

        // Writes "<" + the text of another name + ">", asking the registry again while its own write runs.
        private sealed class NestingProvider : ContextProvider<Item>
        {
            public ContextProviderRegistry? Registry;

            public override bool TryWrite(Item context, TokenArgs args, StringBuilder output)
            {
                if (args.Name != "wrapped") return false;
                var inner = new TokenArgs((Tokens.NamedToken)new Parser().Parse("<%=label%>")[0], context, args.ParentContext);
                output.Append('<').Append(this.Registry!.Token(inner)).Append('>');
                return true;
            }
        }

        [Fact]
        public void TokenCanBeCalledAgainFromInsideAWrite()
        {
            var nesting = new NestingProvider();
            var registry = new ContextProviderRegistry().Register(nesting).Register(new ItemProvider());
            nesting.Registry = registry;
            var item = new Item { Name = "Pen", Count = 3 };
            var args = new TokenArgs((Tokens.NamedToken)new Parser().Parse("<%=wrapped%>")[0], item, new List<object?> { item });

            Assert.Equal("<Pen x3>", registry.Token(args));
            Assert.Equal("[<Pen x3>]", Run(registry, "[<%=wrapped%>]", item));
        }

        [Fact]
        public void TokenReadsGlobalsThatOnlyWriteBeforeAndAfterTheTypedProviders()
        {
            var registry = new ContextProviderRegistry()
                .RegisterGlobal(new YearGlobal(), GlobalOrder.BeforeTyped)
                .RegisterGlobal(new NameGlobal(), GlobalOrder.AfterTyped)
                .RegisterGlobal(new YearGlobal(), GlobalOrder.AfterTyped);
            var parent = new List<object?>();
            TokenArgs Args(string name) => new TokenArgs((Tokens.NamedToken)new Parser().Parse("<%=" + name + "%>")[0], null, parent);

            Assert.Equal("2026", registry.Token(Args("year")));
            Assert.Equal("global", registry.Token(Args("gname")));
            Assert.Null(registry.Token(Args("none")));
        }

        [Fact]
        public void ABlockEndsCleanlyWhenAProviderPushedOntoTheParentContext()
        {
            // Against the rules, a token inside the WITH pushes an extra entry. The block still removes its own entry, the last one equal to it.
            var processor = new Processor
            {
                WithValueProvider = args => "w",
                TokenValueProvider = args =>
                {
                    if (args.Name == "x") args.ParentContext.Add("extra");
                    return string.Join("/", args.ParentContext);
                },
            };

            Assert.Equal("top/w/extra|top/extra", processor.Process(new ProcessorArgs(new Parser().Parse("<%WITH a%><%=x%><%ENDWITH%>|<%=y%>")) { Context = "top" }).ToString());
        }

        [Fact]
        public void WriterWinsOverTheValueProvider()
        {
            var processor = new Processor
            {
                TokenValueProvider = args => "value",
                TokenWriter = (args, output) => output.Append("written"),
            };

            Assert.Equal("written", processor.Process(new ProcessorArgs(new Parser().Parse("<%=a%>"))).ToString());
        }

        [Fact]
        public void SetVariableWinsOverTheWriter()
        {
            var processor = new Processor { TokenWriter = (args, output) => output.Append("written") };

            Assert.Equal("set", processor.Process(new ProcessorArgs(new Parser().Parse("<%SET a%>set<%ENDSET%><%=a%>"))).ToString());
        }

        [Fact]
        public void WriterInsideASetWritesIntoTheSetValue()
        {
            var processor = new Processor { TokenWriter = (args, output) => output.Append(args.Name.Length) };

            Assert.Equal("[3]", processor.Process(new ProcessorArgs(new Parser().Parse("<%SET v%><%=abc%><%ENDSET%>[<%=v%>]"))).ToString());
        }

        [Fact]
        public void AttachToSetsTheWriter()
        {
            var processor = new Processor();
            new ContextProviderRegistry().AttachTo(processor);

            Assert.NotNull(processor.TokenWriter);
        }

        [Fact]
        public void WriteChecksItsArguments()
        {
            var registry = new ContextProviderRegistry();
            var args = new TokenArgs((Tokens.NamedToken)new Parser().Parse("<%=a%>")[0], null, new List<object?>());

            Assert.Throws<ArgumentNullException>(() => registry.Write(null!, new StringBuilder()));
            Assert.Throws<ArgumentNullException>(() => registry.Write(args, null!));
        }
    }
}
