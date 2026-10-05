using System;
using System.Collections.Generic;

using Xunit;

namespace Toshal.Template.Tests
{
    using Toshal.Template;
    using Toshal.Template.Exceptions;
    using Toshal.Template.Tokens;

    public class ProcessTemplateParserTests
    {
        [Fact]
        public void ItShouldBeAbleToProcessTemplate()
        {
            const string mainTemplateText = "This is <%PROCESS_TEMPLATE Sample context=\"abc\"%>";
            var parser = new Parser();

            List<IToken> result = parser.Parse(mainTemplateText);
            Assert.Equal(2, result.Count);
            Assert.IsAssignableFrom<ContentToken>(result[0]);
            Assert.IsAssignableFrom<ProcessTemplateToken>(result[1]);
            Assert.Equal("This is ", ((ContentToken)result[0]).Content);
            Assert.Equal("sample", ((ProcessTemplateToken)result[1]).Name);
            Assert.Equal("abc", ((ProcessTemplateToken)result[1]).GetAttribute("context","xyz"));
        }
    }
}