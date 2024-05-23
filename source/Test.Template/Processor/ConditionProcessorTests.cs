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
		 * If no provider
		 * if with false
		 * if with true
		 * if else with true
		 * if else with false
		 * if else if with true
		 * if else if with false true
		 * if else if with false false
		 * if else if else with false true
		 * if else if else with false false
		 */

        [Test]
        public void BasicConditionToken()
        {
            string templateText = "<%IF ConditionName THEN%>Inner Content<%ENDIF%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            ClassicAssert.AreEqual("", result.ToString());
        }

        [Test]
        public void BasicConditionTokenWithProviderAndFalseValue()
        {
            string templateText = "<%IF ConditionName THEN%>Inner Content<%ENDIF%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.ConditionValueProvider = (ConditionArgs args) => false;
            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            ClassicAssert.AreEqual("", result.ToString());
        }

        [Test]
        public void BasicConditionTokenWithProviderAndTrueValue()
        {
            string templateText = "<%IF ConditionName THEN%>Inner Content<%ENDIF%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.ConditionValueProvider = (ConditionArgs args) => true;
            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            ClassicAssert.AreEqual("Inner Content", result.ToString());
        }

        [Test]
        public void BasicConditionTokenWithProviderReturnBasedOnContext()
        {
            string templateText = "<%IF ConditionName THEN%>Inner Content<%ENDIF%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);
            var user = new TestUser { Name = "My User" };

            var processor = new Processor();
            processor.ConditionValueProvider =
                (ConditionArgs args) => { return ((TestUser)args.Context).Name == "ConditionName"; };

            var process = new ProcessorArgs(tokens);
            process.Context = user;

            StringBuilder result = processor.Process(process);
            ClassicAssert.AreEqual("", result.ToString());

            user = new TestUser { Name = "ConditionName" };
            process = new ProcessorArgs(tokens);
            process.Context = user;

            result = processor.Process(process);
            ClassicAssert.AreEqual("Inner Content", result.ToString());
        }

        [Test]
        public void BasicIfElseConditionTokenAndTrueValue()
        {
            string part1 = "<%IF ";
            string part2 = "ConditionName";
            string part3 = " THEN%>";
            string part4 = "Content within IF";
            string part5 = "<%ELSE%>";
            string part6 = "More content";
            string part7 = "<%ENDIF%>";

            string templateText = part1 + part2 + part3 + part4 + part5 + part6 + part7;

            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.ConditionValueProvider = (ConditionArgs args) => true;

            var process = new ProcessorArgs(tokens);

            StringBuilder result = processor.Process(process);

            ClassicAssert.AreEqual("Content within IF", result.ToString());
        }

        [Test]
        public void BasicIfElseConditionTokenAndFalseValue()
        {
            string part1 = "<%IF ";
            string part2 = "ConditionName";
            string part3 = " THEN%>";
            string part4 = "Content within IF";
            string part5 = "<%ELSE%>";
            string part6 = "More content";
            string part7 = "<%ENDIF%>";

            string templateText = part1 + part2 + part3 + part4 + part5 + part6 + part7;

            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.ConditionValueProvider = (ConditionArgs args) => false;

            var process = new ProcessorArgs(tokens);

            StringBuilder result = processor.Process(process);

            ClassicAssert.AreEqual("More content", result.ToString());
        }

        [Test]
        public void BasicIfElseIfConditionTokenAndFirstConditionTrueValue()
        {
            string part1 = "<%IF ";
            string part2 = "ConditionName";
            string part3 = " THEN%>";
            string part4 = "Content within IF";
            string part5 = "<%ELSEIF OtherCondition THEN%>";
            string part6 = "More content";
            string part7 = "<%ENDIF%>";

            string templateText = part1 + part2 + part3 + part4 + part5 + part6 + part7;

            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.ConditionValueProvider = (ConditionArgs args) => true;

            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);

            ClassicAssert.AreEqual("Content within IF", result.ToString());
        }

        [Test]
        public void BasicIfElseIfConditionTokenAndFirstIsFalseSecondIsTrueValue()
        {
            string part1 = "<%IF ";
            string part2 = "ConditionName";
            string part3 = " THEN%>";
            string part4 = "Content within IF";
            string part5 = "<%ELSEIF OtherCondition THEN%>";
            string part6 = "More content";
            string part9 = "<%ENDIF%>";

            string templateText = part1 + part2 + part3 + part4 + part5 + part6 + part9;

            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.ConditionValueProvider = (ConditionArgs args) =>
            {
                switch (args.Name)
                {
                    case "conditionname":
                        return false;
                    case "othercondition":
                        return true;
                }
                return false;
            };

            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);

            ClassicAssert.AreEqual("More content", result.ToString());
        }

        [Test]
        public void BasicIfElseIfElseConditionTokenForLastElseCheck()
        {
            string part1 = "<%IF ";
            string part2 = "ConditionName";
            string part3 = " THEN%>";
            string part4 = "Content within IF";
            string part5 = "<%ELSEIF OtherCondition THEN%>";
            string part6 = "More content";
            string part9 = "<%ELSE%>";
            string part10 = "Else Content";
            string part11 = "<%ENDIF%>";

            string templateText = part1 + part2 + part3 + part4 + part5 + part6 + part9 + part10 + part11;

            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.ConditionValueProvider = (ConditionArgs args) => false;

            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);

            ClassicAssert.AreEqual("Else Content", result.ToString());
        }

        [Test]
        public void BasicIfElseIfConditionTokenWithAllFalse()
        {
            string part1 = "<%IF ";
            string part2 = "ConditionName";
            string part3 = " THEN%>";
            string part4 = "Content within IF";
            string part5 = "<%ELSEIF OtherCondition THEN%>";
            string part6 = "More content";
            string part7 = "<%ELSEIF OtherCondition THEN%>";
            string part8 = "More content";
            string part9 = "<%ENDIF%>";

            string templateText = part1 + part2 + part3 + part4 + part5 + part6 + part7 + part8 + part9;

            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.ConditionValueProvider = (ConditionArgs args) => false;
            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);

            ClassicAssert.AreEqual("", result.ToString());
        }

        [Test]
        public void BasicNegativeConditionTokenWithProviderAndFalseValue()
        {
            string templateText = "<%IF NOT ConditionName THEN%>Inner Content<%ENDIF%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.ConditionValueProvider = (ConditionArgs args) => false;
            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            ClassicAssert.AreEqual("Inner Content", result.ToString());
        }

        [Test]
        public void BasicNegativeConditionTokenWithProviderAndTrueValue()
        {
            string templateText = "<%IF NOT ConditionName THEN%>Inner Content<%ENDIF%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.ConditionValueProvider = (ConditionArgs args) => true;
            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            ClassicAssert.AreEqual("", result.ToString());
        }
    }
}