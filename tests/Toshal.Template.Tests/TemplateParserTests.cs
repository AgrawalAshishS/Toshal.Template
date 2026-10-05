using System;
using System.Collections.Generic;

using Xunit;

namespace Toshal.Template.Tests
{
    using Toshal.Template;
    using Toshal.Template.Exceptions;
    using Toshal.Template.Tokens;

    public class TemplateParserTests
    {
        [Fact]
        public void ItShouldHandleSimpleText()
        {
            const string templateText = "Simple text";
            var parser = new Parser();

            List<IToken> result = parser.Parse(templateText);
            Assert.Equal(1, result.Count);
            Assert.IsAssignableFrom<ContentToken>(result[0]);
            Assert.Equal(templateText, ((ContentToken)result[0]).Content);
        }

        [Fact]
        public void ItShouldThrowErrorOnUnKnownTag()
        {
            const string templateText = "Simple text<%%>";
            var parser = new Parser();

            Assert.Throws<ParserException>(() =>
            {
                try
                {
                    parser.Parse(templateText);
                }
                catch (ParserException ex)
                {
                    Assert.Equal("<%%>", ex.Split.Content);
                    throw;
                }
            });
        }

        [Fact]
        public void ItShouldHandleBasicToken()
        {
            var parser = new Parser();

            List<IToken> result = parser.Parse("<%=Name%>");
            Assert.Equal(1, result.Count);
            Assert.IsAssignableFrom<NamedToken>(result[0]);
        }

        [Fact]
        public void TokenNameShouldBeLowerCase()
        {
            var parser = new Parser();

            List<IToken> result = parser.Parse("<%=Name%>");
            Assert.Equal(1, result.Count);
            Assert.Equal("name", ((NamedToken)result[0]).Name);
        }

        [Fact]
        public void TokenNameRequired()
        {
            var parser = new Parser();
            Assert.Throws<TokenMissingNameException>(() =>
            {
                try
                {
                    parser.Parse("<%=%>");
                }
                catch (TokenMissingNameException ex)
                {
                    Assert.Equal("<%=%>", ex.Split.Content);
                    throw;
                }
            });

        }

        [Fact]
        public void BasicCombinationOfStringAndToken()
        {
            string part1 = "My basic text ";
            string part2 = "<%=";
            string part3 = "Name";
            string part4 = "%>";
            string part5 = " more text for me to work";
            string part6 = "<%=";
            string part7 = "Name2";
            string part8 = "%>";

            string templateText = part1 + part2 + part3 + part4 + part5 + part6 + part7 + part8;
            var parser = new Parser();

            List<IToken> result = parser.Parse(templateText);
            Assert.Equal(4, result.Count);
            Assert.Equal(part1, ((ContentToken)result[0]).Content);
            Assert.Equal(part3.ToLower(), ((NamedToken)result[1]).Name);
            Assert.Equal(part5, ((ContentToken)result[2]).Content);
            Assert.Equal(part7.ToLower(), ((NamedToken)result[3]).Name);
        }

        [Fact]
        public void ItSouldRespectRegularAngularBreackts()
        {
            const string templateText = "Simple text< %";
            var parser = new Parser();

            List<IToken> result = parser.Parse(templateText);
            Assert.Equal(1, result.Count);
            Assert.IsAssignableFrom<ContentToken>(result[0]);
            Assert.Equal(templateText, ((ContentToken)result[0]).Content);
        }

        [Fact]
        public void ItSouldRespectRegularAngularBreackts2()
        {
            const string templateText = "Simple text<T>";
            var parser = new Parser();

            List<IToken> result = parser.Parse(templateText);
            Assert.Equal(1, result.Count);
            Assert.IsAssignableFrom<ContentToken>(result[0]);
            Assert.Equal(templateText, ((ContentToken)result[0]).Content);
        }

        [Fact]
        public void ItSouldRespectRegularAngularBreackts3()
        {
            const string templateText = "Simple text%%%%%";
            var parser = new Parser();

            List<IToken> result = parser.Parse(templateText);
            Assert.Equal(1, result.Count);
            Assert.IsAssignableFrom<ContentToken>(result[0]);
            Assert.Equal(templateText, ((ContentToken)result[0]).Content);
        }
    }
}