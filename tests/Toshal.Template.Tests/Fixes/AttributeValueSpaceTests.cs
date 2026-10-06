using System;

using Toshal.Template.Exceptions;
using Toshal.Template.Tokens;
using Xunit;

namespace Toshal.Template.Tests.Fixes
{
    // A quoted attribute value may contain spaces, and attributes may be separated by more than one space.
    // Before the fix the parser split the tag on every space, so both threw InvalidTokenAttributeException.
    public class AttributeValueSpaceTests
    {
        private static NamedToken Parse(string template) => (NamedToken)new Parser().Parse(template)[0];

        [Fact]
        public void ValueWithSpaces()
        {
            var token = Parse("<%=Today format=\"dd MMM yyyy\"%>");

            Assert.Equal("today", token.Name);
            Assert.Equal("dd MMM yyyy", token.Attributes["format"]);
        }

        [Fact]
        public void ValueWithLeadingAndTrailingSpacesIsKeptAsIs()
        {
            Assert.Equal(" x ", Parse("<%=Name pad=\" x \"%>").Attributes["pad"]);
        }

        [Fact]
        public void AttributesSeparatedByManySpaces()
        {
            var token = Parse("<%=Name a=\"1\"   b=\"2\"%>");

            Assert.Equal("1", token.Attributes["a"]);
            Assert.Equal("2", token.Attributes["b"]);
        }

        [Fact]
        public void TextAfterTheLastAttributeIsStillAnError()
        {
            Assert.Throws<InvalidTokenAttributeException>(() => Parse("<%=Name a=\"1\" junk%>"));
        }

        // The same attribute twice in one tag is an invalid attribute. Before the fix it threw ArgumentException.
        [Fact]
        public void RepeatedAttributeIsInvalid()
        {
            Assert.Throws<InvalidTokenAttributeException>(() => Parse("<%=Name a=\"1\" a=\"2\"%>"));
        }
    }
}
