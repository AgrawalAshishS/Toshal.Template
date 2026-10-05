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
		 * for without value
		 * for with single value
		 * for with 3 values
		 * for with norecord template - no value
		 * for with norecord template - one value
		 * for header & footer - no value
		 * for header & footer & No record - no value
		 * for header & footer - one value
		 * for header & footer - multiple value
		 * 
		 * for row with 3 values
		 * for row with before row template 3 values
		 * for row with after row template 3 values
		 * for row with before & after row template 3 values
		 * 
		 * for row and alternate row with 3 values
		 * for before after templates for row and alternate 3 values
		 * 
		 * for row and only before-after alternate (no alternate row) with 3 values --- row fallback
		 * for row & alternate row - only before-after row with 3 values -- before-after fallback
		 * 
		 * for first and last row and row template - with 3 values
		 * for first and last row and alternate row template - with 3 values -- alternate row should be applied
		 * for first and last row template - with 3 values (no row content)
		 * 
		 * for before-after first row but no first row and row template -- row fallback
		 * for first row and before-after + row template -- before-after fallback
		 * 
		 * for before-after last row but no last row and row template -- row fallback
		 * for last row and before-after + row template -- before-after fallback
		 * 
		 * for before-after last row but no last row and alternate row + row template with 2 values -- alternate row fallback
		 * for last row and before-after + alternate row + row template with 2 elements -- before-after fallback
		 */

        [Fact]
        public void BasicForEachToken()
        {
            string templateText = "<%FOREACH LoopName%>Inner Content<%ENDFOR%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            Assert.Equal("", result.ToString());
        }

        [Fact]
        public void BasicForEachTokenWithProviderAndNoValue()
        {
            string templateText = "<%FOREACH LoopName%>Inner Content<%ENDFOR%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.LoopValueProvider = (LoopArgs args) => null;
            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            Assert.Equal("", result.ToString());
        }

        [Fact]
        public void BasicForEachTokenWithProviderAndOneValue()
        {
            string templateText = "<%FOREACH LoopName%><%=Value%><%ENDFOR%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.LoopValueProvider = (LoopArgs args) => new List<int> { 1 };
            processor.TokenValueProvider = (TokenArgs args) => args.Context!.ToString();

            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            Assert.Equal("1", result.ToString());
        }

        [Fact]
        public void BasicForEachTokenWithProviderAndMultipleValues()
        {
            string templateText = "<%FOREACH LoopName%><%=Value%><%ENDFOR%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.LoopValueProvider = (LoopArgs args) => new List<int> { 1, 2, 3 };
            processor.TokenValueProvider = (TokenArgs args) => args.Context!.ToString();

            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            Assert.Equal("123", result.ToString());
        }

        [Fact]
        public void ForEachWithNoRecordAndNoValue()
        {
            string templateText = "<%FOREACH LoopName%><%NORECORD%>Some<%ENDNORECORD%><%ENDFOR%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.LoopValueProvider = (LoopArgs args) => null;
            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            Assert.Equal("Some", result.ToString());
        }

        [Fact]
        public void ForEachWithNoRecordAndOneValue()
        {
            string templateText = "<%FOREACH LoopName%><%NORECORD%>Some<%ENDNORECORD%><%ROW%>row content<%ENDROW%><%ENDFOR%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.LoopValueProvider = (LoopArgs args) => new List<int> { 1 };
            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            Assert.Equal("row content", result.ToString());
        }

        [Fact]
        public void ForEachWithNoRecordHeaderFooterAndNoValue()
        {
            string templateText =
                "<%FOREACH LoopName%><%NORECORD%>Nothing<%ENDNORECORD%><%HEADER%>Head<%ENDHEADER%><%FOOTER%>Foot<%ENDFOOTER%><%ENDFOR%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.LoopValueProvider = (LoopArgs args) => null;
            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            Assert.Equal("Nothing", result.ToString());
        }

        [Fact]
        public void ForEachWithHeaderFooterAndNoValue()
        {
            string templateText = "<%FOREACH LoopName%><%HEADER%>Head<%ENDHEADER%><%FOOTER%>Foot<%ENDFOOTER%><%ENDFOR%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.LoopValueProvider = (LoopArgs args) => null;
            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            Assert.Equal("", result.ToString());
        }

        [Fact]
        public void ForEachWithHeaderFooterAndOneValue()
        {
            string templateText = "<%FOREACH LoopName%><%HEADER%>Head<%ENDHEADER%><%FOOTER%>Foot<%ENDFOOTER%><%ENDFOR%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.LoopValueProvider = (LoopArgs args) => new List<int> { 1 };
            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            Assert.Equal("HeadFoot", result.ToString());
        }

        [Fact]
        public void ForEachWithHeaderFooterAndMultipleValue()
        {
            string templateText = "<%FOREACH LoopName%><%HEADER%>Head<%ENDHEADER%><%FOOTER%>Foot<%ENDFOOTER%><%ENDFOR%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.LoopValueProvider = (LoopArgs args) => new List<int> { 1, 2, 3 };
            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            Assert.Equal("HeadFoot", result.ToString());
        }

        [Fact]
        public void ForEachWithRowAndMultipleValues()
        {
            string templateText = "<%FOREACH LoopName%><%ROW%><%=Value%><%ENDROW%><%ENDFOR%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.LoopValueProvider = (LoopArgs args) => new List<int> { 1, 2, 3 };
            processor.TokenValueProvider = (TokenArgs args) => args.Context!.ToString();

            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            Assert.Equal("123", result.ToString());
        }

        [Fact]
        public void ForEachWithRowBeforeAndMultipleValues()
        {
            string templateText = "<%FOREACH LoopName%><%BEFOREROW%>A<%ENDBEFOREROW%><%ROW%><%=Value%><%ENDROW%><%ENDFOR%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.LoopValueProvider = (LoopArgs args) => new List<int> { 1, 2, 3 };
            processor.TokenValueProvider = (TokenArgs args) => args.Context!.ToString();

            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            Assert.Equal("A1A2A3", result.ToString());
        }

        [Fact]
        public void ForEachWithRowAfterAndMultipleValues()
        {
            string templateText = "<%FOREACH LoopName%><%ROW%><%=Value%><%ENDROW%><%AFTERROW%>B<%ENDAFTERROW%><%ENDFOR%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.LoopValueProvider = (LoopArgs args) => new List<int> { 1, 2, 3 };
            processor.TokenValueProvider = (TokenArgs args) => args.Context!.ToString();

            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            Assert.Equal("1B2B3B", result.ToString());
        }

        [Fact]
        public void ForEachWithRowBeforeAfterAndMultipleValues()
        {
            string templateText =
                "<%FOREACH LoopName%><%BEFOREROW%>A<%ENDBEFOREROW%><%ROW%><%=Value%><%ENDROW%><%AFTERROW%>B<%ENDAFTERROW%><%ENDFOR%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.LoopValueProvider = (LoopArgs args) => new List<int> { 1, 2, 3 };
            processor.TokenValueProvider = (TokenArgs args) => args.Context!.ToString();

            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            Assert.Equal("A1BA2BA3B", result.ToString());
        }

        [Fact]
        public void ForEachWithRowAlternateAndMultipleValues()
        {
            string templateText = "<%FOREACH LoopName%><%ROW%><%=Value%>A<%ENDROW%><%ALTROW%><%=Value%>B<%ENDALTROW%><%ENDFOR%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.LoopValueProvider = (LoopArgs args) => new List<int> { 1, 2, 3 };
            processor.TokenValueProvider = (TokenArgs args) => args.Context!.ToString();

            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            Assert.Equal("1A2B3A", result.ToString());
        }

        [Fact]
        public void ForEachWithRowAlternateBeforeAndAfterForBothAndMultipleValues()
        {
            string templateText = "<%FOREACH LoopName%>";
            templateText += "<%BEFOREROW%>-<%ENDBEFOREROW%>";
            templateText += "<%ROW%><%=Value%>A<%ENDROW%>";
            templateText += "<%AFTERROW%>=<%ENDAFTERROW%>";
            templateText += "<%BEFOREALTROW%>+<%ENDBEFOREALTROW%>";
            templateText += "<%ALTROW%><%=Value%>B<%ENDALTROW%>";
            templateText += "<%AFTERALTROW%>!<%ENDAFTERALTROW%>";
            templateText += "<%ENDFOR%>";

            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.LoopValueProvider = (LoopArgs args) => new List<int> { 1, 2, 3 };
            processor.TokenValueProvider = (TokenArgs args) => args.Context!.ToString();

            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            Assert.Equal("-1A=+2B!-3A=", result.ToString());
        }

        [Fact]
        public void ForEachWithRowAlternateBeforeAndAfterForRowOnlyAndMultipleValues()
        {
            string templateText = "<%FOREACH LoopName%>";
            templateText += "<%BEFOREROW%>-<%ENDBEFOREROW%>";
            templateText += "<%ROW%><%=Value%>A<%ENDROW%>";
            templateText += "<%AFTERROW%>=<%ENDAFTERROW%>";
            templateText += "<%ALTROW%><%=Value%>B<%ENDALTROW%>";
            templateText += "<%ENDFOR%>";

            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.LoopValueProvider = (LoopArgs args) => new List<int> { 1, 2, 3 };
            processor.TokenValueProvider = (TokenArgs args) => args.Context!.ToString();

            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            Assert.Equal("-1A=-2B=-3A=", result.ToString());
        }

        [Fact]
        public void ForEachWithRowNoAlternateAndBeforeAndAfterForBothAndMultipleValues()
        {
            string templateText = "<%FOREACH LoopName%>";
            templateText += "<%BEFOREROW%>-<%ENDBEFOREROW%>";
            templateText += "<%ROW%><%=Value%>A<%ENDROW%>";
            templateText += "<%AFTERROW%>=<%ENDAFTERROW%>";
            templateText += "<%BEFOREALTROW%>+<%ENDBEFOREALTROW%>";
            templateText += "<%AFTERALTROW%>!<%ENDAFTERALTROW%>";
            templateText += "<%ENDFOR%>";

            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.LoopValueProvider = (LoopArgs args) => new List<int> { 1, 2, 3 };
            processor.TokenValueProvider = (TokenArgs args) => args.Context!.ToString();

            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            Assert.Equal("-1A=+2A!-3A=", result.ToString());
        }

        [Fact]
        public void ForEachWithFirstLastAndMultipleValues()
        {
            string templateText = "<%FOREACH LoopName%>";
            templateText += "<%FIRSTROW%>-<%=Value%><%ENDFIRSTROW%>";
            templateText += "<%LASTROW%>=<%=Value%><%ENDLASTROW%>";
            templateText += "<%ENDFOR%>";

            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.LoopValueProvider = (LoopArgs args) => new List<int> { 1, 2, 3 };
            processor.TokenValueProvider = (TokenArgs args) => args.Context!.ToString();

            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            Assert.Equal("-1=3", result.ToString());
        }

        [Fact]
        public void ForEachWithFirstLastAndRowAndMultipleValues()
        {
            string templateText = "<%FOREACH LoopName%>";
            templateText += "<%FIRSTROW%>-<%=Value%><%ENDFIRSTROW%>";
            templateText += "<%ROW%>+<%=Value%><%ENDROW%>";
            templateText += "<%LASTROW%>=<%=Value%><%ENDLASTROW%>";
            templateText += "<%ENDFOR%>";

            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.LoopValueProvider = (LoopArgs args) => new List<int> { 1, 2, 3 };
            processor.TokenValueProvider = (TokenArgs args) => args.Context!.ToString();

            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            Assert.Equal("-1+2=3", result.ToString());
        }

        [Fact]
        public void ForEachWithFirstLastAndAltRowAndMultipleValues()
        {
            string templateText = "<%FOREACH LoopName%>";
            templateText += "<%FIRSTROW%>-<%=Value%><%ENDFIRSTROW%>";
            templateText += "<%ALTROW%>+<%=Value%>A<%ENDALTROW%>";
            templateText += "<%LASTROW%>=<%=Value%><%ENDLASTROW%>";
            templateText += "<%ENDFOR%>";

            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.LoopValueProvider = (LoopArgs args) => new List<int> { 1, 2, 3 };
            processor.TokenValueProvider = (TokenArgs args) => args.Context!.ToString();

            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            Assert.Equal("-1+2A=3", result.ToString());
        }

        [Fact]
        public void ForEachWithBeforeAfterFirstAndRowAndMultipleValues()
        {
            string templateText = "<%FOREACH LoopName%>";
            templateText += "<%BEFOREFIRSTROW%>-<%ENDBEFOREFIRSTROW%>";
            templateText += "<%AFTERFIRSTROW%>=<%ENDAFTERFIRSTROW%>";
            templateText += "<%ROW%><%=Value%><%ENDROW%>";
            templateText += "<%ENDFOR%>";

            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.LoopValueProvider = (LoopArgs args) => new List<int> { 1, 2, 3 };
            processor.TokenValueProvider = (TokenArgs args) => args.Context!.ToString();

            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            Assert.Equal("-1=23", result.ToString());
        }

        [Fact]
        public void ForEachWithBeforeAfterRowFirstAndRowAndMultipleValues()
        {
            string templateText = "<%FOREACH LoopName%>";
            templateText += "<%FIRSTROW%><%=Value%>F<%ENDFIRSTROW%>";
            templateText += "<%BEFOREROW%>-<%ENDBEFOREROW%>";
            templateText += "<%ROW%><%=Value%><%ENDROW%>";
            templateText += "<%AFTERROW%>!<%ENDAFTERROW%>";
            templateText += "<%ENDFOR%>";

            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.LoopValueProvider = (LoopArgs args) => new List<int> { 1, 2, 3 };
            processor.TokenValueProvider = (TokenArgs args) => args.Context!.ToString();

            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            Assert.Equal("-1F!-2!-3!", result.ToString());
        }

        [Fact]
        public void ForEachWithBeforeAfterLastAndRowAndMultipleValues()
        {
            string templateText = "<%FOREACH LoopName%>";
            templateText += "<%BEFORELASTROW%>-<%ENDBEFORELASTROW%>";
            templateText += "<%AFTERLASTROW%>=<%ENDAFTERLASTROW%>";
            templateText += "<%ROW%><%=Value%><%ENDROW%>";
            templateText += "<%ENDFOR%>";

            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.LoopValueProvider = (LoopArgs args) => new List<int> { 1, 2, 3 };
            processor.TokenValueProvider = (TokenArgs args) => args.Context!.ToString();

            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            Assert.Equal("12-3=", result.ToString());
        }

        [Fact]
        public void ForEachWithBeforeAfterRowLastAndRowAndMultipleValues()
        {
            string templateText = "<%FOREACH LoopName%>";
            templateText += "<%LASTROW%><%=Value%>L<%ENDLASTROW%>";
            templateText += "<%BEFOREROW%>-<%ENDBEFOREROW%>";
            templateText += "<%ROW%><%=Value%><%ENDROW%>";
            templateText += "<%AFTERROW%>!<%ENDAFTERROW%>";
            templateText += "<%ENDFOR%>";

            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.LoopValueProvider = (LoopArgs args) => new List<int> { 1, 2, 3 };
            processor.TokenValueProvider = (TokenArgs args) => args.Context!.ToString();

            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            Assert.Equal("-1!-2!-3L!", result.ToString());
        }

        [Fact]
        public void ForEachWithBeforeAfterLastAndAltRowAndMultipleValues()
        {
            string templateText = "<%FOREACH LoopName%>";
            templateText += "<%BEFORELASTROW%>-<%ENDBEFORELASTROW%>";
            templateText += "<%AFTERLASTROW%>=<%ENDAFTERLASTROW%>";
            templateText += "<%ALTROW%><%=Value%><%ENDALTROW%>";
            templateText += "<%ENDFOR%>";

            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.LoopValueProvider = (LoopArgs args) => new List<int> { 1, 2 };
            processor.TokenValueProvider = (TokenArgs args) => args.Context!.ToString();

            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            Assert.Equal("-2=", result.ToString());
        }

        [Fact]
        public void ForEachWithBeforeAfterRowLastAndAltRowAndMultipleValues()
        {
            string templateText = "<%FOREACH LoopName%>";
            templateText += "<%LASTROW%><%=Value%>L<%ENDLASTROW%>";
            templateText += "<%BEFOREALTROW%>-<%ENDBEFOREALTROW%>";
            templateText += "<%ROW%><%=Value%><%ENDROW%>";
            templateText += "<%AFTERALTROW%>!<%ENDAFTERALTROW%>";
            templateText += "<%ENDFOR%>";

            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.LoopValueProvider = (LoopArgs args) => new List<int> { 1, 2 };
            processor.TokenValueProvider = (TokenArgs args) => args.Context!.ToString();

            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            Assert.Equal("1-2L!", result.ToString());
        }

        [Fact]
        public void ForEachWithBeforeAfterFirstLastAndRowAndMultipleValues()
        {
            string templateText = "<%FOREACH LoopName%>";
            templateText += "<%BEFOREFIRSTROW%>-<%ENDBEFOREFIRSTROW%>";
            templateText += "<%AFTERFIRSTROW%>=<%ENDAFTERFIRSTROW%>";
            templateText += "<%ROW%><%=Value%><%ENDROW%>";
            templateText += "<%BEFORELASTROW%>+<%ENDBEFORELASTROW%>";
            templateText += "<%AFTERLASTROW%>!<%ENDAFTERLASTROW%>";
            templateText += "<%ENDFOR%>";

            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.LoopValueProvider = (LoopArgs args) => new List<int> { 1, 2, 3 };
            processor.TokenValueProvider = (TokenArgs args) => args.Context!.ToString();

            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            Assert.Equal("-1=2+3!", result.ToString());
        }


        [Fact]
        public void ForEachParentContextMaintained()
        {
            string templateText = "<%FOREACH LoopName%>";
            templateText += "<%HEADER%><%=my_count%><%ENDHEADER%>";
            templateText += "<%ROW%><%=Value%><%ENDROW%>";
            templateText += "<%ENDFOR%>";

            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.LoopValueProvider = (LoopArgs args) => new List<int> { 1, 2, 3 };
            processor.TokenValueProvider = (TokenArgs args) =>
            {
                if (args.Name == "my_count")
                {
                    Assert.Equal(2, args.ParentContext.Count);
                    Assert.IsAssignableFrom<List<int>>(args.Context!);
                    return ((List<int>)args.Context!).Count.ToString();
                }
                Assert.Equal(3, args.ParentContext.Count);
                return args.Context!.ToString();
            };

            var process = new ProcessorArgs(tokens);
            process.Context = "ParentContext";

            StringBuilder result = processor.Process(process);
            Assert.Equal("3123", result.ToString());
        }
    }
}