using System.Collections.Generic;
using System.Text;

using NUnit.Framework;
using NUnit.Framework.Legacy;
using Test.Template.SupportClass;
using Toshal.Template;
using Toshal.Template.Tokens;

namespace Test.Template
{
    [TestFixture]
    public class RemovePreviousCharTests
    {

        [Test]
        public void TestingNewLineRemovalCode1()
        {
            string part1 = @"My basic text 
<%REMOVE_PREVIOUS 2%>
";

            string templateText = part1;
            var parser = new Parser();

            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            StringBuilder result = processor.Process(new ProcessorArgs(tokens));
            ClassicAssert.AreEqual(@"My basic text 
", result.ToString());
        }

        [Test]
        public void TestingNewLineRemovalCode2()
        {
            string part1 = @"My basic text 
<%REMOVE_PREVIOUS 3%>";

            string templateText = part1;
            var parser = new Parser();

            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            StringBuilder result = processor.Process(new ProcessorArgs(tokens));
            ClassicAssert.AreEqual(@"My basic text", result.ToString());
        }

        [Test]
        public void TestingNewLineRemovalCode3()
        {
            string part1 = @"<%REMOVE_PREVIOUS 5%>";

            string templateText = part1;
            var parser = new Parser();

            List<IToken> tokens = parser.Parse(templateText);
            var processor = new Processor();
            StringBuilder result = processor.Process(new ProcessorArgs(tokens));
            ClassicAssert.AreEqual(@"", result.ToString());
        }

        [Test]
        public void TestingNewLineRemovalCode4()
        {
            string part1 = @"My basic text <%REMOVE_PREVIOUS 2%>";

            string templateText = part1;
            var parser = new Parser();

            List<IToken> tokens = parser.Parse(templateText);
            var processor = new Processor();
            StringBuilder result = processor.Process(new ProcessorArgs(tokens));
            ClassicAssert.AreEqual(@"My basic tex", result.ToString());
        }

        [Test]
        public void TestingNewLineRemovalCode5()
        {
            string part1 = @"
My basic text <%REMOVE_PREVIOUS 20%>";

            string templateText = part1;
            var parser = new Parser();

            List<IToken> tokens = parser.Parse(templateText);
            var processor = new Processor();
            StringBuilder result = processor.Process(new ProcessorArgs(tokens));
            ClassicAssert.AreEqual(@"", result.ToString());
        }

        [Test]
        public void TestingNewLineRemovalCode6()
        {
            string part1 = @"My basic text
<%IF NOT ABC THEN%><%REMOVE_PREVIOUS 2%><%ENDIF%>";

            string templateText = part1;
            var parser = new Parser();

            List<IToken> tokens = parser.Parse(templateText);
            var processor = new Processor();
            processor.ConditionValueProvider = (arg) => false;
            StringBuilder result = processor.Process(new ProcessorArgs(tokens));
            ClassicAssert.AreEqual(@"My basic text", result.ToString());
        }
    }
}
