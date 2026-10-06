using System;
using System.Collections.Generic;
using System.Text;

using Xunit;

namespace Toshal.Template.Tests
{
    public class ProcessIntoBuilderTests
    {
        private static readonly Processor Processor = new Processor
        {
            TokenValueProvider = args => args.Name,
            LoopValueProvider = args => new List<int> { 1, 2 },
        };

        private static void Process(string template, StringBuilder output)
        {
            Processor.Process(new ProcessorArgs(new Parser().Parse(template)), output);
        }

        [Fact]
        public void AppendsToTheTextAlreadyInTheBuilder()
        {
            var output = new StringBuilder("start:");

            Process("<%=a%>-<%=b%>", output);

            Assert.Equal("start:a-b", output.ToString());
        }

        [Fact]
        public void TwoCallsAppendOneAfterTheOther()
        {
            var output = new StringBuilder();

            Process("<%=a%>", output);
            Process("<%=b%>", output);

            Assert.Equal("ab", output.ToString());
        }

        [Fact]
        public void GivesTheSameTextAsTheOtherOverload()
        {
            const string template = "<%FOREACH l%><%CONTEXT_AS_STRING%>, <%ENDFOR%><%REMOVE_PREVIOUS 2%>!";
            var output = new StringBuilder();

            Process(template, output);

            Assert.Equal(Processor.Process(new ProcessorArgs(new Parser().Parse(template))).ToString(), output.ToString());
        }

        [Fact]
        public void RemovePreviousDoesNotRemoveTextFromBeforeTheCall()
        {
            var output = new StringBuilder("keep");

            Process("ab<%REMOVE_PREVIOUS 5%>c", output);

            Assert.Equal("keepc", output.ToString());
        }

        [Fact]
        public void RemovePreviousNewLineDoesNotRemoveANewLineFromBeforeTheCall()
        {
            var output = new StringBuilder("line\r\n");

            Process("<%REMOVE_PREVIOUS_NEW_LINE%>x", output);

            Assert.Equal("line\r\nx", output.ToString());
        }

        [Fact]
        public void RemovePreviousNewLineStopsAtTheStartOfTheCall()
        {
            // The \r was there before the call, the \n was written by it: only the \n goes.
            var output = new StringBuilder("line\r");

            Process("\n<%REMOVE_PREVIOUS_NEW_LINE%>x", output);

            Assert.Equal("line\rx", output.ToString());
        }

        [Fact]
        public void RemovePreviousInsideASetStillWorksOnTheSetValue()
        {
            var output = new StringBuilder("keep");

            Process("<%SET v%>abc<%REMOVE_PREVIOUS 1%><%ENDSET%>[<%=v%>]", output);

            Assert.Equal("keep[ab]", output.ToString());
        }

        [Fact]
        public void NullArgumentsThrow()
        {
            Assert.Throws<ArgumentNullException>(() => Processor.Process(null!, new StringBuilder()));
            Assert.Throws<ArgumentNullException>(() => Processor.Process(new ProcessorArgs(new Parser().Parse("x")), null!));
        }
    }
}
