using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace Test.Template
{
    using Toshal.Template;
    using Toshal.Template.Exceptions;
    using Toshal.Template.Tokens;

    [TestFixture]
    public class ProcessTemplateParserTests
    {
        [Test]
        public void ItShouldBeAbleToProcessTemplate()
        {
            const string mainTemplateText = "This is <%PROCESS_TEMPLATE Sample context=\"abc\"%>";
            var parser = new Parser();

            List<IToken> result = parser.Parse(mainTemplateText);
            Assert.AreEqual(2, result.Count);
            Assert.IsInstanceOf<ContentToken>(result[0]);
            Assert.IsInstanceOf<ProcessTemplateToken>(result[1]);
            Assert.AreEqual("This is ", ((ContentToken)result[0]).Content);
            Assert.AreEqual("sample", ((ProcessTemplateToken)result[1]).Name);
            Assert.AreEqual("abc", ((ProcessTemplateToken)result[1]).GetAttribute("context","xyz"));
        }
    }
}