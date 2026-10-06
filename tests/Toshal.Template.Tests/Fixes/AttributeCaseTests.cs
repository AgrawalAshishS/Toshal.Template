using Toshal.Template.Tokens;
using Xunit;

namespace Toshal.Template.Tests.Fixes
{
    // Owner decision: names that the library uses (the tag name, the FOREACH name of REUSE_FOREACH) and attribute names are lower case.
    // Attribute values keep the case the template author wrote, in every tag. The parser also fills a lower case copy of the values.
    // Before: IF, ELSEIF and FOREACH lower cased their attribute values, the other tags did not.
    public class AttributeCaseTests
    {
        private static T First<T>(string template) => (T)new Parser().Parse(template)[0];

        [Fact]
        public void IfKeepsTheValueCase()
        {
            var token = First<ConditionToken>("<%IF Weight Unit=\"KG\"%>x<%ENDIF%>");

            Assert.Equal("weight", token.Name);
            Assert.Equal("KG", token.Attributes["unit"]);
        }

        [Fact]
        public void ElseIfKeepsTheValueCase()
        {
            var token = First<ConditionToken>("<%IF a%>x<%ELSEIF Weight unit=\"KG\"%>y<%ENDIF%>");
            var elseIf = Assert.IsType<ConditionToken>(token.FalsePart);

            Assert.Equal("weight", elseIf.Name);
            Assert.Equal("KG", elseIf.Attributes["unit"]);
        }

        [Fact]
        public void NotIsStillFoundInAnyCase()
        {
            var token = First<ConditionToken>("<%IF NOT Paid Level=\"Gold\" THEN%>x<%ENDIF%>");

            Assert.False(token.IsPositive);
            Assert.Equal("paid", token.Name);
            Assert.Equal("Gold", token.Attributes["level"]);
        }

        [Fact]
        public void ForEachKeepsTheValueCase()
        {
            var token = First<ForEachToken>("<%FOREACH Lines Sort=\"Name\"%>x<%ENDFOR%>");

            Assert.Equal("lines", token.Name);
            Assert.Equal("Name", token.Attributes["sort"]);
        }

        [Fact]
        public void ParserFillsALowerCaseCopyOfTheValues()
        {
            var token = First<NamedToken>("<%=Total Format=\"N2\" Currency=\"INR\"%>");

            Assert.Equal("N2", token.Attributes["format"]);
            Assert.Equal("n2", token.Attributes.LowerCaseValues["format"]);
            Assert.Equal("inr", token.Attributes.GetLowerCaseValue("CURRENCY", "x"));
            Assert.Equal("x", token.Attributes.GetLowerCaseValue("missing", "x"));
        }

        [Fact]
        public void ProvidersSeeBothValues()
        {
            string? seen = null;
            var processor = new Processor
            {
                ConditionValueProvider = args =>
                {
                    seen = args.Attributes.GetValue("unit", "") + "/" + args.Attributes.GetLowerCaseValue("unit", "");
                    return true;
                },
            };

            processor.Process(new ProcessorArgs(new Parser().Parse("<%IF Weight unit=\"KG\"%>x<%ENDIF%>")));

            Assert.Equal("KG/kg", seen);
        }
    }
}
