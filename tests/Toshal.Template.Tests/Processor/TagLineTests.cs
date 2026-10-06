using System.Collections.Generic;

using Xunit;

namespace Toshal.Template.Tests
{
    // A line that holds only control tags and spaces or tabs writes nothing: not its indent, not its line break.
    // Lines with text or with a tag that writes something stay as they are.
    public class TagLineTests
    {
        private static string Run(string template)
        {
            var processor = new Processor
            {
                ConditionValueProvider = args => args.Name == "yes",
                LoopValueProvider = args => new List<string> { "a", "b" },
                WithValueProvider = args => "w",
                TokenValueProvider = args => args.Name,
                ProcessTemplateValueProvider = args => new Parser().Parse("sub"),
            };

            return processor.Process(new ProcessorArgs(new Parser().Parse(template))).ToString();
        }

        [Fact]
        public void IfLinesDisappear()
        {
            Assert.Equal("start\n  inside\nend\n", Run("start\n<%IF yes%>\n  inside\n<%ENDIF%>\nend\n"));
        }

        [Fact]
        public void IndentedTagLinesDisappearWithTheirIndent()
        {
            Assert.Equal("class C\n{\n    int a;\n    int b;\n}\n",
                Run("class C\n{\n    <%FOREACH cols%>\n    int <%CONTEXT_AS_STRING%>;\n    <%ENDFOR%>\n}\n"));
        }

        [Fact]
        public void CrLfLineBreaksGoToo()
        {
            Assert.Equal("start\r\n  inside\r\nend\r\n", Run("start\r\n<%IF yes%>\r\n  inside\r\n<%ENDIF%>\r\nend\r\n"));
        }

        [Fact]
        public void TabsAndSpacesAroundTheTagGo()
        {
            Assert.Equal("x\ny\n", Run("x\n \t<%IF yes%> \t\ny\n\t<%ENDIF%>  \n"));
        }

        [Fact]
        public void ElseAndElseIfLinesDisappear()
        {
            Assert.Equal("no\n", Run("<%IF no%>\nyes\n<%ELSEIF other%>\nother\n<%ELSE%>\nno\n<%ENDIF%>\n"));
        }

        [Fact]
        public void SeveralControlTagsOnOneLineDisappearTogether()
        {
            Assert.Equal("a\nb\n", Run("<%FOREACH l%><%ROW%>\n<%CONTEXT_AS_STRING%>\n<%ENDROW%>  <%ENDFOR%>\n"));
        }

        [Fact]
        public void ForEachPartLinesDisappear()
        {
            Assert.Equal("head\na\nb\nfoot\n",
                Run("<%FOREACH l%>\n<%HEADER%>\nhead\n<%ENDHEADER%>\n<%ROW%>\n<%CONTEXT_AS_STRING%>\n<%ENDROW%>\n<%FOOTER%>\nfoot\n<%ENDFOOTER%>\n<%ENDFOR%>\n"));
        }

        [Fact]
        public void WithAndSetLinesDisappear()
        {
            Assert.Equal("w\nvalue\n", Run("<%WITH x%>\n<%CONTEXT_AS_STRING%>\n<%ENDWITH%>\n<%SET v%>\nvalue\n<%ENDSET%>\n<%=v%>"));
        }

        [Fact]
        public void FirstAndLastLinesOfTheTemplateCount()
        {
            Assert.Equal("inside\n", Run("<%IF yes%>\ninside\n<%ENDIF%>"));
            Assert.Equal("inside\n", Run("  <%IF yes%>  \ninside\n  <%ENDIF%>  "));
        }

        [Fact]
        public void ATagOnlyTemplateWritesNothing()
        {
            Assert.Equal("", Run("<%IF yes%><%ENDIF%>"));
            Assert.Equal("", Run("\t<%IF yes%>\n<%ENDIF%>\n"));
        }

        [Fact]
        public void BlankLinesStay()
        {
            Assert.Equal("a\n\nb\n", Run("a\n\n<%IF yes%>\nb\n<%ENDIF%>\n"));
        }

        [Fact]
        public void TextOnTheLineKeepsTheLine()
        {
            Assert.Equal("x: y\n", Run("x: <%IF yes%>y<%ENDIF%>\n"));
            Assert.Equal("[\ny\n]\n", Run("[<%IF yes%>\ny\n<%ENDIF%>]\n"));
        }

        [Fact]
        public void TagsThatWriteSomethingKeepTheLine()
        {
            Assert.Equal("  name\n", Run("  <%=name%>\n"));
            Assert.Equal("  w\n", Run("<%WITH x%>\n  <%CONTEXT_AS_STRING%>\n<%ENDWITH%>\n"));
            Assert.Equal("  sub\n", Run("  <%PROCESS_TEMPLATE s%>\n"));
            Assert.Equal("  name x\n", Run("  <%=name%><%IF yes%> x<%ENDIF%>\n"));
        }

        [Fact]
        public void RemovePreviousLinesKeepTheirOldBehavior()
        {
            Assert.Equal("a, b\n", Run("<%FOREACH l%><%CONTEXT_AS_STRING%>, <%ENDFOR%>\n<%REMOVE_PREVIOUS 3%>\n"));
            Assert.Equal("ab", Run("a\n<%REMOVE_PREVIOUS_NEW_LINE%>b"));
        }

        [Fact]
        public void ReuseForEachLineIsKept()
        {
            Assert.Equal("ab\nab\n", Run("<%FOREACH l%><%CONTEXT_AS_STRING%><%ENDFOR%>\n<%REUSE_FOREACH l again%>\n"));
        }

        [Fact]
        public void TwoTagLinesInARow()
        {
            Assert.Equal("x\n", Run("<%IF yes%>\n<%IF yes%>\nx\n<%ENDIF%>\n<%ENDIF%>\n"));
        }

        [Fact]
        public void LineNumbersOfTokensStayTheSourceLines()
        {
            var tokens = new Parser().Parse("<%IF yes%>\n  <%=name%>\n<%ENDIF%>\n");
            var name = ((Tokens.ConditionToken)tokens[0]).InnerTokens[1];

            Assert.Equal(2, name.LineNumber);
            Assert.Equal(3, name.StartingPosition);
        }
    }
}
