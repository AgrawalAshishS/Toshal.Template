using System.Collections.Generic;
using System.Text;

using Xunit;
using Toshal.Template.Tests.SupportClass;

namespace Toshal.Template.Tests
{
    using Toshal.Template;
    using Toshal.Template.Tokens;

    public partial class TemplateProcessorTests
    {
        /*
		 * reuse one level (2nd element) within for loop
		 * reuse outside loop
		 */

        [Fact]
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
            Assert.Equal("1 K:2 K:3 K:4 K:", result.ToString());
        }

        [Fact]
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
            Assert.Equal("1234", result.ToString());
        }
    }
}