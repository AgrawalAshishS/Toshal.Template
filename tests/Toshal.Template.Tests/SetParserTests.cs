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
    public class SetParserTests
    {
        [Test]
        public void ItShouldRecognizeSet()
        {
            const string templateText = "<%SET MyTokenName %>MyValue<%ENDSET%><%=MyTokenName%>";
            var parser = new Parser();
            List<IToken> result = parser.Parse(templateText);

            ClassicAssert.AreEqual(2, result.Count);
            var token = (SetToken)result[0];
            ClassicAssert.AreEqual("MyTokenName".ToLower(), token.Name);
            ClassicAssert.AreEqual(1, token.InnerTokens.Count);

            var namedToken = (NamedToken)result[1];
            ClassicAssert.AreEqual("MyTokenName".ToLower(), namedToken.Name);
        }

        [Test]
        public void EmptyValueShouldNotThrowError()
        {
            const string templateText = "<%SET MyTokenName %><%ENDSET%>";
            var parser = new Parser();
            List<IToken> result = parser.Parse(templateText);

            ClassicAssert.AreEqual(1, result.Count);
            var token = (SetToken)result[0];
            ClassicAssert.AreEqual("MyTokenName".ToLower(), token.Name);
            ClassicAssert.AreEqual(0, token.InnerTokens.Count);
        }
    }
}
