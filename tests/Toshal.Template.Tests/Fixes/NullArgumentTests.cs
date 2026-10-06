using System;
using System.Collections.Generic;

using Toshal.Template.Tokens;
using Xunit;

namespace Toshal.Template.Tests.Fixes
{
    // Public entry points reject null with ArgumentNullException that names the parameter,
    // instead of failing later with NullReferenceException.
    public class NullArgumentTests
    {
        [Fact]
        public void ParseRejectsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new Parser().Parse(null!));
            Assert.Equal("templateText", ex.ParamName);
        }

        [Fact]
        public void ProcessRejectsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new Processor().Process(null!));
            Assert.Equal("args", ex.ParamName);
        }

        [Fact]
        public void ProcessorArgsRejectsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new ProcessorArgs(null!));
            Assert.Equal("tokenList", ex.ParamName);
        }

        [Fact]
        public void GetValueRejectsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new TokenAttributeDictionary().GetValue(null!, "x"));
            Assert.Equal("attributeName", ex.ParamName);
        }

        // A REUSE_FOREACH token made by hand is not linked to a FOREACH. Processing it is a usage error with a clear message.
        [Fact]
        public void UnlinkedReuseForEachThrowsInvalidOperation()
        {
            var tokens = new List<IToken> { new ReuseForEachToken(new Split { Content = "<%REUSE_FOREACH a b%>" }) };
            var processor = new Processor { LoopValueProvider = args => new[] { 1 } };

            Assert.Throws<InvalidOperationException>(() => processor.Process(new ProcessorArgs(tokens)));
        }
    }
}
