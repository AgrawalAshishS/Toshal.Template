using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Toshal.Template.Providers;
using Xunit;

namespace Toshal.Template.Tests.Providers
{
    public class ContextProviderRegistryTests
    {
        private class Animal { public string Name { get; set; } = ""; }

        private class Dog : Animal { public bool Barks { get; set; } = true; public List<string> Toys { get; } = new() { "ball", "rope" }; public Animal? Friend { get; set; } }

        private interface IOwned { string Owner { get; } }

        private class OwnedDog : Dog, IOwned { public string Owner { get; set; } = "Asha"; }

        private sealed class DogProvider : ContextProvider<Dog>
        {
            public int Calls;

            public override bool TryToken(Dog context, TokenArgs args, out string? value)
            {
                this.Calls++;
                switch (args.Name)
                {
                    case "name": value = context.Name; return true;
                    case "kind": value = "dog"; return true;
                }

                value = null;
                return false;
            }

            public override bool TryCondition(Dog context, ConditionArgs args, out bool value)
            {
                switch (args.Name)
                {
                    case "barks": value = context.Barks; return true;
                }

                value = false;
                return false;
            }

            public override bool TryLoop(Dog context, LoopArgs args, out IList? value)
            {
                switch (args.Name)
                {
                    case "toys": value = context.Toys; return true;
                }

                value = null;
                return false;
            }

            public override bool TryWith(Dog context, TokenArgs args, out object? value)
            {
                switch (args.Name)
                {
                    case "friend": value = context.Friend; return true;
                }

                value = null;
                return false;
            }
        }

        // Answers names with a fixed text, to see which provider won.
        private sealed class FixedProvider<T> : ContextProvider<T> where T : class
        {
            private readonly string name;
            private readonly string? text;
            private readonly bool? condition;

            public FixedProvider(string name, string? text, bool? condition = null)
            {
                this.name = name;
                this.text = text;
                this.condition = condition;
            }

            public override bool TryToken(T context, TokenArgs args, out string? value)
            {
                value = this.text;
                return args.Name == this.name;
            }

            public override bool TryCondition(T context, ConditionArgs args, out bool value)
            {
                value = this.condition ?? false;
                return args.Name == this.name && this.condition.HasValue;
            }
        }

        private sealed class FixedGlobal : GlobalProvider
        {
            private readonly string name;
            private readonly string text;

            public FixedGlobal(string name, string text)
            {
                this.name = name;
                this.text = text;
            }

            public override bool TryToken(TokenArgs args, out string? value)
            {
                value = this.text;
                return args.Name == this.name;
            }

            public override bool TryCondition(ConditionArgs args, out bool value)
            {
                value = true;
                return args.Name == this.name;
            }

            public override bool TryLoop(LoopArgs args, out IList? value)
            {
                value = new[] { this.text };
                return args.Name == this.name;
            }

            public override bool TryWith(TokenArgs args, out object? value)
            {
                value = this.text;
                return args.Name == this.name;
            }
        }

        private static string Run(ContextProviderRegistry registry, string template, object? context)
        {
            var processor = new Processor();
            registry.AttachTo(processor);
            return processor.Process(new ProcessorArgs(new Parser().Parse(template)) { Context = context }).ToString();
        }

        [Fact]
        public void TypedProviderAnswersEveryKindOfTag()
        {
            var registry = new ContextProviderRegistry().Register(new DogProvider());
            var dog = new Dog { Name = "Rex", Friend = new Dog { Name = "Max" } };

            string text = Run(registry, "<%=name%><%IF barks%> woof<%ENDIF%>:<%FOREACH toys%> <%CONTEXT_AS_STRING%><%ENDFOR%> <%WITH friend%><%=name%><%ENDWITH%>", dog);

            Assert.Equal("Rex woof: ball rope Max", text);
        }

        [Fact]
        public void FirstRegisteredProviderWinsEvenWithAnEmptyValue()
        {
            var registry = new ContextProviderRegistry()
                .Register(new FixedProvider<Dog>("name", ""))
                .Register(new DogProvider());

            Assert.Equal("[]", Run(registry, "[<%=name%>]", new Dog { Name = "Rex" }));
        }

        [Fact]
        public void HandledFalseConditionStopsTheSearch()
        {
            var registry = new ContextProviderRegistry()
                .Register(new FixedProvider<Dog>("barks", null, condition: false))
                .Register(new DogProvider());

            Assert.Equal("quiet", Run(registry, "<%IF barks%>woof<%ELSE%>quiet<%ENDIF%>", new Dog { Barks = true }));
        }

        [Fact]
        public void UnhandledNameGoesToTheNextProvider()
        {
            var registry = new ContextProviderRegistry()
                .Register(new FixedProvider<Dog>("other", "x"))
                .Register(new DogProvider());

            Assert.Equal("Rex", Run(registry, "<%=name%>", new Dog { Name = "Rex" }));
        }

        [Fact]
        public void BaseClassProviderAnswersForADerivedContext()
        {
            var registry = new ContextProviderRegistry().Register(new FixedProvider<Animal>("name", "animal"));

            Assert.Equal("animal", Run(registry, "<%=name%>", new Dog()));
        }

        [Fact]
        public void NearestTypeIsTriedBeforeItsBaseTypeWhateverTheOrderOfRegistration()
        {
            var registry = new ContextProviderRegistry()
                .Register(new FixedProvider<Animal>("name", "animal"))
                .Register(new FixedProvider<Dog>("name", "dog"));

            Assert.Equal("dog", Run(registry, "<%=name%>", new OwnedDog()));
            Assert.Equal("animal", Run(registry, "<%=name%>", new Animal()));
        }

        [Fact]
        public void InterfaceProvidersComeAfterClassProviders()
        {
            var registry = new ContextProviderRegistry()
                .Register(new FixedProvider<IOwned>("name", "owned"))
                .Register(new FixedProvider<IOwned>("owner", "Asha"))
                .Register(new FixedProvider<Animal>("name", "animal"));

            Assert.Equal("animal Asha", Run(registry, "<%=name%> <%=owner%>", new OwnedDog()));
        }

        [Fact]
        public void ObjectProviderAnswersForAnyContext()
        {
            var registry = new ContextProviderRegistry().Register(new FixedProvider<object>("name", "any"));

            Assert.Equal("any", Run(registry, "<%=name%>", new Dog()));
            Assert.Equal("any", Run(registry, "<%=name%>", "text"));
            Assert.Equal("any", Run(registry, "<%=name%>", 42));
        }

        [Fact]
        public void GlobalBeforeTypedWinsOverTypedProviders()
        {
            var registry = new ContextProviderRegistry()
                .Register(new DogProvider())
                .RegisterGlobal(new FixedGlobal("name", "global"), GlobalOrder.BeforeTyped);

            Assert.Equal("global", Run(registry, "<%=name%>", new Dog { Name = "Rex" }));
        }

        [Fact]
        public void GlobalAfterTypedIsUsedOnlyWhenNoTypedProviderAnswers()
        {
            var registry = new ContextProviderRegistry()
                .Register(new DogProvider())
                .RegisterGlobal(new FixedGlobal("name", "global"))
                .RegisterGlobal(new FixedGlobal("year", "2026"));

            Assert.Equal("Rex 2026", Run(registry, "<%=name%> <%=year%>", new Dog { Name = "Rex" }));
        }

        [Fact]
        public void GlobalsAnswerEveryKindOfTag()
        {
            var registry = new ContextProviderRegistry().RegisterGlobal(new FixedGlobal("g", "v"));

            Assert.Equal("v yes v v", Run(registry, "<%=g%> <%IF g%>yes<%ENDIF%> <%FOREACH g%><%CONTEXT_AS_STRING%><%ENDFOR%> <%WITH g%><%CONTEXT_AS_STRING%><%ENDWITH%>", null));
        }

        [Fact]
        public void NullContextAsksOnlyTheGlobals()
        {
            var dogs = new DogProvider();
            var registry = new ContextProviderRegistry()
                .Register(dogs)
                .Register(new FixedProvider<object>("name", "any"))
                .RegisterGlobal(new FixedGlobal("year", "2026"));

            Assert.Equal("[] 2026", Run(registry, "[<%=name%>] <%=year%>", null));
            Assert.Equal(0, dogs.Calls);
        }

        [Fact]
        public void UnhandledNamesGiveTheDefaultValues()
        {
            var registry = new ContextProviderRegistry().Register(new DogProvider());
            var dog = new Dog();
            var parent = new List<object?> { dog };
            var tokens = new Parser().Parse("<%=x%><%IF x%><%ENDIF%><%FOREACH x%><%ENDFOR%><%WITH x%><%ENDWITH%>");

            Assert.Null(registry.Token(new TokenArgs((Tokens.NamedToken)tokens[0], dog, parent)));
            Assert.False(registry.Condition(new ConditionArgs((Tokens.ConditionToken)tokens[1], dog, parent)));
            Assert.Null(registry.Loop(new LoopArgs((Tokens.ForEachToken)tokens[2], dog, parent)));
            Assert.Null(registry.With(new TokenArgs((Tokens.WithToken)tokens[3], dog, parent)));
        }

        [Fact]
        public void ContextWithoutProvidersWritesNothing()
        {
            var registry = new ContextProviderRegistry().Register(new DogProvider());

            Assert.Equal("[]", Run(registry, "[<%=name%>]", "text"));
        }

        [Fact]
        public void RegisterAfterTheFirstLookupThrows()
        {
            var registry = new ContextProviderRegistry().Register(new DogProvider());
            Run(registry, "<%=name%>", new Dog());

            Assert.Throws<InvalidOperationException>(() => registry.Register(new FixedProvider<Animal>("a", "b")));
            Assert.Throws<InvalidOperationException>(() => registry.RegisterGlobal(new FixedGlobal("a", "b")));
        }

        [Fact]
        public void RegisterNullThrows()
        {
            var registry = new ContextProviderRegistry();

            Assert.Throws<ArgumentNullException>(() => registry.Register<Dog>(null!));
            Assert.Throws<ArgumentNullException>(() => registry.RegisterGlobal(null!));
            Assert.Throws<ArgumentNullException>(() => registry.AttachTo(null!));
        }

        [Fact]
        public void AttachToSetsTheFourProvidersAndKeepsProcessTemplate()
        {
            var registry = new ContextProviderRegistry();
            var processor = new Processor { ProcessTemplateValueProvider = args => null };

            registry.AttachTo(processor);

            Assert.NotNull(processor.TokenValueProvider);
            Assert.NotNull(processor.ConditionValueProvider);
            Assert.NotNull(processor.LoopValueProvider);
            Assert.NotNull(processor.WithValueProvider);
            Assert.NotNull(processor.ProcessTemplateValueProvider);
        }

        [Fact]
        public void ManyThreadsGetTheSameAnswers()
        {
            var registry = new ContextProviderRegistry()
                .Register(new FixedProvider<Animal>("name", "animal"))
                .Register(new FixedProvider<Dog>("name", "dog"))
                .Register(new FixedProvider<IOwned>("owner", "owner"));
            var tokens = new Parser().Parse("<%=name%>-<%=owner%>");
            var contexts = new object[] { new Animal(), new Dog(), new OwnedDog(), "text" };
            var expected = new[] { "animal-", "dog-", "dog-owner", "-" };

            Parallel.For(0, 2000, i =>
            {
                var processor = new Processor();
                registry.AttachTo(processor);
                var index = i % contexts.Length;
                var text = processor.Process(new ProcessorArgs(tokens) { Context = contexts[index] }).ToString();
                Assert.Equal(expected[index], text);
            });
        }
    }
}
