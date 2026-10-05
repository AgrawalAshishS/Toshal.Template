using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace Test.Template
{
    using NUnit.Framework.Legacy;
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
            ClassicAssert.AreEqual(2, result.Count);
            ClassicAssert.IsInstanceOf<ContentToken>(result[0]);
            ClassicAssert.IsInstanceOf<ProcessTemplateToken>(result[1]);
            ClassicAssert.AreEqual("This is ", ((ContentToken)result[0]).Content);
            ClassicAssert.AreEqual("sample", ((ProcessTemplateToken)result[1]).Name);
            ClassicAssert.AreEqual("abc", ((ProcessTemplateToken)result[1]).GetAttribute("context","xyz"));
        }
    }
}