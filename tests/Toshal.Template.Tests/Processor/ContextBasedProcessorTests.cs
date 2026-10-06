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

        /*context based tests*/

        [Fact]
        public void BasicNamedTokenTemplateWithValuePickedFromContext()
        {
            string templateText = "<%=Name%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);
            var user = new TestUser { Name = "My User" };

            var processor = new Processor();
            processor.TokenValueProvider = (TokenArgs args) =>
            {
                switch (args.Name)
                {
                    case "name":
                        return ((TestUser)args.Context!).Name;
                }
                return null;
            };

            var process = new ProcessorArgs(tokens);
            process.Context = user;
            StringBuilder result = processor.Process(process);
            Assert.Equal("My User", result.ToString());
        }

        [Fact]
        public void BasicIfElseIfConditionTokenWithProviderAndFalseValueReturnBasedOnContext()
        {
            string part1 = "<%IF ";
            string part2 = "IsTestUser";
            string part3 = " THEN%>";
            string part4 = "Content within IF";
            string part5 = "<%ELSEIF OtherCondition THEN%>";
            string part6 = "More content";
            string part7 = "<%ENDIF%>";

            string templateText = part1 + part2 + part3 + part4 + part5 + part6 + part7;

            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.ConditionValueProvider =
                (ConditionArgs args) => { return ((dynamic)args.Context!).Name == "Test User"; };

            var process = new ProcessorArgs(tokens);
            process.Context = new { Name = "Wrong User" };

            StringBuilder result = processor.Process(process);
            Assert.Equal("", result.ToString());
        }

        [Fact]
        public void BasicForEachTokenWithProviderReturnBasedOnContext()
        {
            string templateText = "<%FOREACH LoopName%><%=Name%><%ENDFOR%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);
            var user1 = new TestUser { Name = "First Name" };
            var user2 = new TestUser { Name = "Last Name" };

            var processor = new Processor();

            processor.TokenValueProvider = (TokenArgs args) => ((TestUser)args.Context!).Name;
            processor.LoopValueProvider = (LoopArgs args) =>
            {
                //in case of user1 context return user2 object.
                if (((TestUser)args.Context!).Name == "First Name")
                    return new List<TestUser> { user2 };

                return new List<TestUser> { user1 };
            };

            var process = new ProcessorArgs(tokens);
            process.Context = user1;
            StringBuilder result = processor.Process(process);
            Assert.Equal("Last Name", result.ToString());

            process = new ProcessorArgs(tokens);
            process.Context = user2;

            result = processor.Process(process);
            Assert.Equal("First Name", result.ToString());
        }

        [Fact]
        public void ItShouldProcessLastRowTokenWhenThereIsOnlyOneRowForLoop()
        {
            string templateText = "<%FOREACH LoopName%>";
            templateText += "<%BEFOREROW%>-<%ENDBEFOREROW%>";
            templateText += "<%ROW%><%=Value%><%ENDROW%>";
            templateText += "<%AFTERROW%>=<%ENDAFTERROW%>";
            templateText += "<%BEFORELASTROW%>+<%ENDBEFORELASTROW%>";
            templateText += "<%AFTERLASTROW%>!<%ENDAFTERLASTROW%>";
            templateText += "<%ENDFOR%>";

            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.LoopValueProvider = (LoopArgs args) => new List<int> { 1 };
            processor.TokenValueProvider = (TokenArgs args) => args.Context!.ToString();

            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            Assert.Equal("+1!", result.ToString());
        }
    }
}