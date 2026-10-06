using System.Collections.Generic;
using System.Text;

using Xunit;
using Toshal.Template.Tests.SupportClass;
using Toshal.Template;
using Toshal.Template.Tokens;

namespace Toshal.Template.Tests
{
    public class RemovePreviousLineTests
    {

        [Fact]
        public void TestingNewLineRemovalCode1()
        {
            string part1 = @"My basic text 
<%REMOVE_PREVIOUS_NEW_LINE%>
";

            string templateText = part1;
            var parser = new Parser();

            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            StringBuilder result = processor.Process(new ProcessorArgs(tokens));
            Assert.Equal(@"My basic text 
", result.ToString());
        }

        [Fact]
        public void TestingNewLineRemovalCode2()
        {
            string part1 = @"My basic text 
<%REMOVE_PREVIOUS_NEW_LINE%>";

            string templateText = part1;
            var parser = new Parser();

            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            StringBuilder result = processor.Process(new ProcessorArgs(tokens));
            Assert.Equal(@"My basic text ", result.ToString());
        }

        [Fact]
        public void TestingNewLineRemovalCode3()
        {
            string part1 = @"<%REMOVE_PREVIOUS_NEW_LINE%>";

            string templateText = part1;
            var parser = new Parser();

            List<IToken> tokens = parser.Parse(templateText);
            var processor = new Processor();
            StringBuilder result = processor.Process(new ProcessorArgs(tokens));
            Assert.Equal(@"", result.ToString());
        }

        [Fact]
        public void TestingNewLineRemovalCode4()
        {
            string part1 = @"My basic text <%REMOVE_PREVIOUS_NEW_LINE%>";

            string templateText = part1;
            var parser = new Parser();

            List<IToken> tokens = parser.Parse(templateText);
            var processor = new Processor();
            StringBuilder result = processor.Process(new ProcessorArgs(tokens));
            Assert.Equal(@"My basic text ", result.ToString());
        }

        [Fact]
        public void TestingNewLineRemovalCode5()
        {
            string part1 = @"
My basic text <%REMOVE_PREVIOUS_NEW_LINE%>";

            string templateText = part1;
            var parser = new Parser();

            List<IToken> tokens = parser.Parse(templateText);
            var processor = new Processor();
            StringBuilder result = processor.Process(new ProcessorArgs(tokens));
            Assert.Equal(@"
My basic text ", result.ToString());
        }

        [Fact]
        public void TestingNewLineRemovalCode6()
        {
            string part1 = @"My basic text
<%IF NOT ABC THEN%><%REMOVE_PREVIOUS_NEW_LINE%><%ENDIF%>";

            string templateText = part1;
            var parser = new Parser();

            List<IToken> tokens = parser.Parse(templateText);
            var processor = new Processor();
            processor.ConditionValueProvider = (arg) => false;
            StringBuilder result = processor.Process(new ProcessorArgs(tokens));
            Assert.Equal(@"My basic text", result.ToString());
        }
    }
}
