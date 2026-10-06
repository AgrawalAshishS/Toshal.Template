using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using Xunit;

namespace Toshal.Template.Tests.ParserGolden
{
    // Pins the exact output of the parser for tricky templates: names, attributes, positions, and the error for each bad tag.
    // ParserGolden.txt was recorded from the parser before it was rewritten for speed; the rewrite must give the same text.
    // To record again (only when a change of behavior is approved): set TOSHAL_RECORD_GOLDEN=1 and run this test.
    public class ParserGoldenTests
    {
        public static readonly string[] Cases =
        {
            // Plain text and splitting
            "",
            "plain text",
            "line 1\r\nline 2\nline 3",
            "a %> b",
            "%>",
            "%>%>",
            "a <% b",
            "<",
            "%",
            "<%",
            "<%%>",
            "100% <sure>",
            "a<%=x%>b<%=y%>c",
            "<%=a <%=b%>",
            "<%=a%>%>",
            "<%=a%%>",
            "<%=a%><%=b%>\r\n<%=c%>",
            "\n\n  <%=deep%>",
            "<%=a\nb%>",
            "<%=a\rb%>",
            "<%=a\tb%>",
            "x\r\n<%=a%>\r\n\ty <%=b%>",

            // Values
            "<%=Name%>",
            "<%= Name %>",
            "<%=%>",
            "<%= %>",
            "<%=\t%>",
            "<%=FirstName LastName%>",
            "<%=a=b%>",
            "<%=\u00DCn\u00EFcode%>",
            "<%=\u03A3\u0399\u03A3\u03A5\u03A6\u039F\u03A3%>",
            "<%=\u0130stanbul%>",
            "<%=a\u00A0b%>",
            "<%=\u00A0a\u00A0%>",
            "<%=a",
            "abc <%= a",

            // Attributes
            "<%=Total format=\"0.00\"%>",
            "<%=Total FORMAT=\"Upper Case\"%>",
            "<%=Total format=\"a b\" pad=\"5\"%>",
            "<%=Total format=\"a\"pad=\"5\"%>",
            "<%=Total  format=\"x\"%>",
            "<%=Total format=\"x\" %>",
            "<%=Total format=\"\"%>",
            "<%=Total format=\"x\" format=\"y\"%>",
            "<%=Total format=\"x\" FORMAT=\"y\"%>",
            "<%=Total format=x%>",
            "<%=Total format=\"x\" junk%>",
            "<%=Total junk format=\"x\"%>",
            "<%=Total format = \"x\"%>",
            "<%=Total format=\"x%>",
            "<%=Total format=\"a=b\"%>",
            "<%=Total f_1=\"x\" \u00FC=\"y\"%>",
            "<%=Total format=\"x\"\tpad=\"1\"%>",
            "<%=Total\tformat=\"x\"%>",
            "<%=Total format=\"x\"\u00A0pad=\"1\"%>",
            "<%=Total format=\"x\"\u2028pad=\"1\"%>",
            "<%=Total a-b=\"1\"%>",
            "<%=Total format=\"\u0130I\"%>",

            // IF
            "<%IF a%>x<%ENDIF%>",
            "<%IF a%>x<%ELSE%>y<%ENDIF%>",
            "<%IF a%>x<%ELSEIF b%>y<%ELSEIF not c%>z<%ELSE%>w<%ENDIF%>",
            "<%IF a THEN%>x<%ENDIF%>",
            "<%IF not a THEN%>x<%ENDIF%>",
            "<%IF a\tTHEN%>x<%ENDIF%>",
            "<%IF a THEN b%>x<%ENDIF%>",
            "<%IF a THEN b THEN%>x<%ENDIF%>",
            "<%IF  THEN%>x<%ENDIF%>",
            "<%IF THEN%>x<%ENDIF%>",
            "<%IF a then%>x<%ENDIF%>",
            "<%IF NOT a%>x<%ENDIF%>",
            "<%IF Not  a%>x<%ENDIF%>",
            "<%IF  not a%>x<%ENDIF%>",
            "<%IF notA%>x<%ENDIF%>",
            "<%IF not%>x<%ENDIF%>",
            "<%IF not %>x<%ENDIF%>",
            "<%IF not not a%>x<%ENDIF%>",
            "<%IF a value=\"1\" condition=\"GT\"%>x<%ENDIF%>",
            "<%IF not a value=\"1\" THEN%>x<%ENDIF%>",
            "<%IF %>x<%ENDIF%>",
            "<%IF\ta%>x<%ENDIF%>",
            "<%IFa%>x<%ENDIF%>",
            "<%if a%>x<%endif%>",
            "<%IF a%>x",
            "<%IF a%>x<%ELSE%>y",
            "<%IF a%>x<%ELSEIF%>y<%ENDIF%>",
            "<%IF a%>x<%ELSEIF b THEN%>y<%ENDIF%>",
            "<%IF a%>x<%ELSE%>y<%ELSE%>z<%ENDIF%>",
            "<%IF a%>x<%ENDIF %>",
            "<%IF a\nb%>x<%ENDIF%>",
            "<%IF a\n THEN%>x<%ENDIF%>",
            "<%IF a \nTHEN%>x<%ENDIF%>",
            "<%IF a%><%IF b%>x<%ENDIF%><%ENDIF%>",
            "<%ENDIF%>",
            "<%ELSE%>",

            // FOREACH
            "<%FOREACH rows%>x<%ENDFOR%>",
            "<%FOREACH rows%><%ROW%>r<%ENDROW%><%ENDFOR%>",
            "<%FOREACH rows%><%NORECORD%>n<%ENDNORECORD%><%HEADER%>h<%ENDHEADER%><%BEFOREFIRSTROW%>1<%ENDBEFOREFIRSTROW%><%FIRSTROW%>2<%ENDFIRSTROW%><%AFTERFIRSTROW%>3<%ENDAFTERFIRSTROW%><%BEFOREROW%>4<%ENDBEFOREROW%><%ROW%>5<%ENDROW%><%AFTERROW%>6<%ENDAFTERROW%><%BEFOREALTROW%>7<%ENDBEFOREALTROW%><%ALTROW%>8<%ENDALTROW%><%AFTERALTROW%>9<%ENDAFTERALTROW%><%BEFORELASTROW%>a<%ENDBEFORELASTROW%><%LASTROW%>b<%ENDLASTROW%><%AFTERLASTROW%>c<%ENDAFTERLASTROW%><%FOOTER%>d<%ENDFOOTER%><%ENDFOR%>",
            "<%FOREACH rows%><%FOOTER%>f<%ENDFOOTER%><%HEADER%>h<%ENDHEADER%><%ENDFOR%>",
            "<%FOREACH rows%>a<%ROW%>r<%ENDROW%>b<%ENDFOR%>",
            "<%FOREACH rows%><%ROW%>r<%ENDROW%><%ROW%>s<%ENDROW%><%ENDFOR%>",
            "<%FOREACH rows%><%ROW%>r<%ENDFOR%>",
            "<%FOREACH rows%><%ROW%>r<%ENDALTROW%><%ENDFOR%>",
            "<%FOREACH rows%><%UNKNOWN%><%ENDFOR%>",
            "<%FOREACH rows%><%ELSE%><%ENDFOR%>",
            "<%FOREACH rows%>x",
            "<%FOREACH %>x<%ENDFOR%>",
            "<%FOREACH  %>x<%ENDFOR%>",
            "<%FOREACH rows sort=\"name\"%>x<%ENDFOR%>",
            "<%FOREACH Rows%>x<%ENDFOR%><%FOREACH rows%>y<%ENDFOR%>",
            "<%FOREACH rows%><%FOREACH rows%>x<%ENDFOR%><%ENDFOR%>",
            "<%FOREACH\trows%>x<%ENDFOR%>",
            "<%FOREACH rows%>\r\n  <%ROW%>\r\n    <%=name%>\r\n  <%ENDROW%>\r\n<%ENDFOR%>",
            "<%FOREACH rows%><%HEADER%><%FOREACH inner%><%ROW%>i<%ENDROW%><%ENDFOR%><%ENDHEADER%><%ENDFOR%>",

            // REUSE_FOREACH
            "<%FOREACH rows%>x<%ENDFOR%><%REUSE_FOREACH rows again%>",
            "<%REUSE_FOREACH rows again%><%FOREACH rows%>x<%ENDFOR%>",
            "<%REUSE_FOREACH Rows  Again%><%FOREACH rows%>x<%ENDFOR%>",
            "<%REUSE_FOREACH  rows again%><%FOREACH rows%>x<%ENDFOR%>",
            "<%REUSE_FOREACH rows again extra%><%FOREACH rows%>x<%ENDFOR%>",
            "<%REUSE_FOREACH rows\tagain%><%FOREACH rows%>x<%ENDFOR%>",
            "<%REUSE_FOREACH rows\nagain%><%FOREACH rows%>x<%ENDFOR%>",
            "<%REUSE_FOREACH rows%><%FOREACH rows%>x<%ENDFOR%>",
            "<%REUSE_FOREACH rows %><%FOREACH rows%>x<%ENDFOR%>",
            "<%REUSE_FOREACH missing again%>",
            "<%WITH a%><%FOREACH rows%>x<%ENDFOR%><%ENDWITH%><%REUSE_FOREACH rows again%>",
            "<%FOREACH rows%>1<%ENDFOR%><%WITH a%><%FOREACH rows%>2<%ENDFOR%><%REUSE_FOREACH rows again%><%ENDWITH%>",

            // WITH and SET
            "<%WITH customer%><%=name%><%ENDWITH%>",
            "<%WITH  customer %>x<%ENDWITH%>",
            "<%WITH %>x<%ENDWITH%>",
            "<%WITH  %>x<%ENDWITH%>",
            "<%WITH customer mode=\"x\"%>y<%ENDWITH%>",
            "<%WITH customer%>x",
            "<%WITH customer%>x<%ENDIF%>",
            "<%SET Title%>Hello <%=name%><%ENDSET%><%=title%>",
            "<%SET %>x<%ENDSET%>",
            "<%SET a b=\"1\"%>x<%ENDSET%>",
            "<%SET a%>x",
            "<%SET a%>x<%ENDWITH%>",

            // Small tags
            "<%REMOVE_PREVIOUS 2%>",
            "<%REMOVE_PREVIOUS  2 %>",
            "<%REMOVE_PREVIOUS 0%>",
            "<%REMOVE_PREVIOUS -1%>",
            "<%REMOVE_PREVIOUS x%>",
            "<%REMOVE_PREVIOUS %>",
            "<%REMOVE_PREVIOUS 99999999999%>",
            "<%REMOVE_PREVIOUS +3%>",
            "<%REMOVE_PREVIOUS\t2%>",
            "<%REMOVE_PREVIOUS 2",
            "<%REMOVE_PREVIOUS_NEW_LINE%>",
            "<%REMOVE_PREVIOUS_NEW_LINE %>",
            "<%CONTEXT_AS_STRING%>",
            "<%CONTEXT_AS_STRING %>",
            "<%PROCESS_TEMPLATE footer%>",
            "<%PROCESS_TEMPLATE Footer lang=\"EN\"%>",
            "<%PROCESS_TEMPLATE %>",
            "<%PROCESS_TEMPLATE\tfooter%>",
            "<%UNKNOWN%>",
            "<%ENDFOR%>",
            "<% =a%>",
            // Invisible characters are not ignored: before, the culture compare read these as tags.
            "<%IF\u00AD a%>x<%ENDIF%>",
            "\u00AD<\u200B%>text",
        };

        [Fact]
        public void ParserOutputIsUnchanged()
        {
            string actual = Record();
            string file = GoldenFile();

            if (Environment.GetEnvironmentVariable("TOSHAL_RECORD_GOLDEN") == "1")
            {
                File.WriteAllText(file, actual);
                return;
            }

            string expected = File.ReadAllText(file).Replace("\r\n", "\n");
            if (expected == actual) return;

            // Show the first case that differs.
            var expectedCases = expected.Split("### ");
            var actualCases = actual.Split("### ");
            for (var i = 0; i < Math.Min(expectedCases.Length, actualCases.Length); i++)
            {
                Assert.Equal(expectedCases[i], actualCases[i]);
            }

            Assert.Equal(expectedCases.Length, actualCases.Length);
        }

        private static string Record()
        {
            var text = new StringBuilder();
            for (var i = 0; i < Cases.Length; i++)
            {
                text.Append("### ").Append(i).Append(' ').Append(TokenDump.Escape(Cases[i])).Append('\n');
                text.Append(TokenDump.Parse(Cases[i]));
            }

            return text.ToString();
        }

        private static string GoldenFile([CallerFilePath] string source = "")
        {
            return Path.Combine(Path.GetDirectoryName(source)!, "ParserGolden.txt");
        }
    }
}
