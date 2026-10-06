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
		 * with change in default context
		 * with change in for context
		 * with and if condition context
		 * With in case of Null return will not render
		 */

        private string? UserTokenDataProvider(TokenArgs args)
        {
            var arg = args.Context as TestUser;
            if (arg == null) return null;

            switch (args.Name)
            {
                case "name":
                    return arg.Name;
            }
            return null;
        }

        private string? ProfileTokenDataProvider(TokenArgs args)
        {
            var arg = args.Context as TestUserProfile;
            if (arg == null) return null;

            switch (args.Name)
            {
                case "address":
                    return arg.Address;
            }
            return null;
        }

        private bool? UserConditionValueProvider(ConditionArgs args)
        {
            var arg = args.Context as TestUser;
            if (arg == null) return null;

            switch (args.Name)
            {
                case "hasname":
                    return string.IsNullOrEmpty(arg.Name) == false;
            }
            return null;
        }

        private bool? ProfileConditionValueProvider(ConditionArgs args)
        {
            var arg = args.Context as TestUserProfile;
            if (arg == null) return null;

            switch (args.Name)
            {
                case "hasaddress":
                    return string.IsNullOrEmpty(arg.Address) == false;
            }
            return null;
        }

        [Fact]
        public void WithTokenNoProvider()
        {
            string templateText = "Name:<%=Name%><%WITH Profile%>, Address: <%=Address%><%ENDWITH%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);
            var user = new TestUser { Name = "My User", Profile = new TestUserProfile { Address = "abcd" } };

            var processor = new Processor();
            processor.TokenValueProvider = (TokenArgs args) =>
            {
                string? retVal = UserTokenDataProvider(args);
                if (retVal == null) retVal = ProfileTokenDataProvider(args);

                return retVal;
            };

            var process = new ProcessorArgs(tokens);
            process.Context = user;
            StringBuilder result = processor.Process(process);
            Assert.Equal("Name:My User", result.ToString());
        }

        [Fact]
        public void WithTokenProviderButNullReturn()
        {
            string templateText = "Name:<%=Name%><%WITH Profile%>, Address: <%=Address%><%ENDWITH%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);
            var user = new TestUser { Name = "My User", Profile = new TestUserProfile { Address = "abcd" } };

            var processor = new Processor();
            processor.WithValueProvider = (TokenArgs args) => null;
            processor.TokenValueProvider = (TokenArgs args) =>
            {
                string? retVal = UserTokenDataProvider(args);
                if (retVal == null) retVal = ProfileTokenDataProvider(args);

                return retVal;
            };

            var process = new ProcessorArgs(tokens);
            process.Context = user;
            StringBuilder result = processor.Process(process);
            Assert.Equal("Name:My User", result.ToString());
        }

        [Fact]
        public void WithTokenValueOverrideDefaultContextNamedToken()
        {
            string templateText = "Name:<%=Name%><%WITH Profile%>, Address: <%=Address%><%ENDWITH%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);
            var user = new TestUser { Name = "My User", Profile = new TestUserProfile { Address = "abcd" } };

            var processor = new Processor();
            processor.WithValueProvider = (TokenArgs args) => ((TestUser)args.Context!).Profile;
            processor.TokenValueProvider = (TokenArgs args) =>
            {
                string? retVal = UserTokenDataProvider(args);
                if (retVal == null) retVal = ProfileTokenDataProvider(args);

                return retVal;
            };

            var process = new ProcessorArgs(tokens);
            process.Context = user;
            StringBuilder result = processor.Process(process);
            Assert.Equal("Name:My User, Address: abcd", result.ToString());
        }

        [Fact]
        public void WithTokenValueOverrideDefaultContextConditionToken()
        {
            string templateText =
                "<%IF HasName THEN%>Name:<%=Name%><%ENDIF%><%WITH Profile%><%IF HasAddress THEN%>, Address: <%=Address%><%ENDIF%><%ENDWITH%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);
            var user = new TestUser { Name = "My User", Profile = new TestUserProfile { Address = "abcd" } };

            var processor = new Processor();
            processor.WithValueProvider = (TokenArgs args) => ((TestUser)args.Context!).Profile;
            processor.ConditionValueProvider = (ConditionArgs args) =>
            {
                bool? retVal = UserConditionValueProvider(args);
                if (retVal == null) retVal = ProfileConditionValueProvider(args);

                return retVal != null && retVal.Value;
            };

            processor.TokenValueProvider = (TokenArgs args) =>
            {
                string? retVal = UserTokenDataProvider(args);
                if (retVal == null) retVal = ProfileTokenDataProvider(args);

                return retVal;
            };

            var process = new ProcessorArgs(tokens);
            process.Context = user;
            StringBuilder result = processor.Process(process);
            Assert.Equal("Name:My User, Address: abcd", result.ToString());
        }

        [Fact]
        public void WithTokenValueOverrideDefaultContextForEachToken()
        {
            string templateText =
                "<%FOREACH User%>Name:<%=Name%><%WITH Profile%><%IF HasAddress THEN%>, Address: <%=Address%><%ENDIF%><%ENDWITH%><%ENDFOR%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);
            var user = new TestUser { Name = "My User", Profile = new TestUserProfile { Address = "abcd" } };

            var processor = new Processor();
            processor.LoopValueProvider = (LoopArgs args) => new List<TestUser> { user };
            processor.WithValueProvider = (TokenArgs args) => ((TestUser)args.Context!).Profile;
            processor.ConditionValueProvider = (ConditionArgs args) =>
            {
                bool? retVal = UserConditionValueProvider(args);
                if (retVal == null) retVal = ProfileConditionValueProvider(args);

                return retVal != null && retVal.Value;
            };

            processor.TokenValueProvider = (TokenArgs args) =>
            {
                string? retVal = UserTokenDataProvider(args);
                if (retVal == null) retVal = ProfileTokenDataProvider(args);

                return retVal;
            };

            var process = new ProcessorArgs(tokens);
            process.Context = user;
            StringBuilder result = processor.Process(process);
            Assert.Equal("Name:My User, Address: abcd", result.ToString());
        }
    }
}