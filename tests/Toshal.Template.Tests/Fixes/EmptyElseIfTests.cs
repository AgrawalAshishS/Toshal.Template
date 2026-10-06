using Xunit;

namespace Toshal.Template.Tests.Fixes
{
    // An ELSEIF with no content must still pass control to the next ELSEIF or ELSE.
    // Before the fix the whole false part was skipped when the first ELSEIF had no content.
    public class EmptyElseIfTests
    {
        private static string Run(string template, params string[] trueConditions)
        {
            var processor = new Processor { ConditionValueProvider = args => System.Array.IndexOf(trueConditions, args.Name) >= 0 };
            return processor.Process(new ProcessorArgs(new Parser().Parse(template))).ToString();
        }

        [Fact]
        public void ElseRunsAfterAnEmptyElseIf()
        {
            Assert.Equal("C", Run("<%IF a%>A<%ELSEIF b%><%ELSE%>C<%ENDIF%>"));
        }

        [Fact]
        public void SecondElseIfRunsAfterAnEmptyElseIf()
        {
            Assert.Equal("D", Run("<%IF a%>A<%ELSEIF b%><%ELSEIF d%>D<%ENDIF%>", "d"));
        }

        [Fact]
        public void EmptyElseIfThatIsTrueWritesNothing()
        {
            Assert.Equal("", Run("<%IF a%>A<%ELSEIF b%><%ELSE%>C<%ENDIF%>", "b"));
        }
    }
}
