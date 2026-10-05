using System.Collections.Generic;
using System.Text;

using NUnit.Framework;
using Test.Template.SupportClass;

namespace Test.Template
{
    using NUnit.Framework.Legacy;
    using Toshal.Template;
    using Toshal.Template.Tokens;

    [TestFixture]
    public partial class TemplateProcessorTests
    {
        /*
		 * reuse one level (2nd element) within for loop
		 * reuse outside loop
		 */

        [Test]
        public void ReUseForEachWithinLoop()
        {
            string templateText = "<%FOREACH Users%><%=Name%> K:<%REUSE_FOREACH Users Related%><%ENDFOR%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.TokenValueProvider = (TokenArgs args) =>
            {
                string retVal = UserTokenDataProvider(args);
                if (retVal == null) retVal = ProfileTokenDataProvider(args);

                return retVal;
            };

            processor.LoopValueProvider = (LoopArgs args) =>
            {
                switch (args.Name)
                {
                    case "users":
                        return new List<TestUser> { new TestUser { Name = "1" }, new TestUser { Name = "2" } };
                    case "related":
                        if (((TestUser)args.Context).Name == "2")
                            return new List<TestUser> { new TestUser { Name = "3" }, new TestUser { Name = "4" } };
                        break;
                }

                return null;
            };

            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            ClassicAssert.AreEqual("1 K:2 K:3 K:4 K:", result.ToString());
        }

        [Test]
        public void ReUseForEachOutsideLoop()
        {
            string templateText = "<%FOREACH Users%><%=Name%><%ENDFOR%><%REUSE_FOREACH Users Related%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.TokenValueProvider = (TokenArgs args) =>
            {
                string retVal = UserTokenDataProvider(args);
                if (retVal == null) retVal = ProfileTokenDataProvider(args);

                return retVal;
            };

            processor.LoopValueProvider = (LoopArgs args) =>
            {
                switch (args.Name)
                {
                    case "users":
                        return new List<TestUser> { new TestUser { Name = "1" }, new TestUser { Name = "2" } };
                    case "related":
                        return new List<TestUser> { new TestUser { Name = "3" }, new TestUser { Name = "4" } };
                }

                return null;
            };

            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            ClassicAssert.AreEqual("1234", result.ToString());
        }
    }
}