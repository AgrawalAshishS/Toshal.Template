using System;
using System.Text.RegularExpressions;

using Toshal.Template.Exceptions;
using Toshal.Template.Tokens;
using Xunit;

namespace Toshal.Template.Tests.ParserGolden
{
    // The parser reads attributes without Regex. These tests check every character against the Regex classes the parser used before:
    // \w for attribute names and \s between attributes.
    public class CharacterClassTests
    {
        private static readonly Regex Word = new Regex(@"^\w$");
        private static readonly Regex Space = new Regex(@"^\s$");

        // Characters that end a tag or a line, or start the value, change the tag itself, so they are not part of this check.
        private static bool Skip(char c) => c == '\n' || c == '%' || c == '<' || c == '"' || c == '=';

        [Fact]
        public void AttributeNamesUseTheRegexWordClass()
        {
            for (var i = 0; i <= char.MaxValue; i++)
            {
                var c = (char)i;
                if (Skip(c)) continue;

                var template = "<%=a k" + c + "=\"1\"%>";
                if (Word.IsMatch(c.ToString()))
                {
                    var token = (NamedToken)new Parser().Parse(template)[0];
                    Assert.True(token.Attributes.ContainsKey(("k" + c).ToLower()), $"U+{i:X4}");
                }
                else
                {
                    Assert.ThrowsAny<ParserException>(() => new Parser().Parse(template));
                }
            }
        }

        [Fact]
        public void SpaceBetweenAttributesUsesTheRegexSpaceClass()
        {
            for (var i = 0; i <= char.MaxValue; i++)
            {
                var c = (char)i;
                if (Skip(c)) continue;

                var template = "<%=a b=\"1\"" + c + "d=\"2\"%>";
                if (Space.IsMatch(c.ToString()))
                {
                    var token = (NamedToken)new Parser().Parse(template)[0];
                    Assert.True(token.Attributes.ContainsKey("d"), $"U+{i:X4}");
                }
                else if (Word.IsMatch(c.ToString()))
                {
                    // b="1"xd="2": the next name simply starts right after the quote.
                    var token = (NamedToken)new Parser().Parse(template)[0];
                    Assert.True(token.Attributes.ContainsKey((c + "d").ToLower()), $"U+{i:X4}");
                }
                else
                {
                    Assert.ThrowsAny<ParserException>(() => new Parser().Parse(template));
                }
            }
        }
    }
}
