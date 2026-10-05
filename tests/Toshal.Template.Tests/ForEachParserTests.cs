using System;
using System.Collections.Generic;
using Xunit;

namespace Toshal.Template.Tests
{
    using Toshal.Template;
    using Toshal.Template.Exceptions;
    using Toshal.Template.Tokens;

    public class ForEachParserTests
    {
        //TODO: Add For each tests for multiple occurrence of inner elements.

        [Fact]
        public void ItShouldHandleBasicForEach()
        {
            string templateText = "<%FOREACH Name %>";
            templateText += "Content within for";
            templateText += "<%ENDFOR%>";

            var parser = new Parser();
            List<IToken> result = parser.Parse(templateText);

            Assert.Equal(1, result.Count);
            var token = (ForEachToken)result[0];
            Assert.Equal("name", token.Name);
            Assert.Equal(1, token.RowTokens.Count);

            var content = (ContentToken)token.RowTokens[0];
            Assert.Equal("Content within for", content.Content);
        }

        [Fact]
        public void ItShouldThrowErrorWhenClosingEndForMissing1()
        {
            string templateText = "<%FOREACH Name %>";
            templateText += "Content within for";

            var parser = new Parser();
            Assert.Throws<TokenNotClosedException>(() =>
            {
                try
                {
                    parser.Parse(templateText);
                }
                catch (TokenNotClosedException ex)
                {
                    Assert.Equal("name", ex.TokenName);
                    Assert.Equal("<%FOREACH Name %>", ex.Split.Content);
                    throw;
                }
            });
        }

        [Fact]
        public void ItShouldThrowErrorWhenForEachNameMissing()
        {
            string templateText = "<%FOREACH %>";

            var parser = new Parser();
            Assert.Throws<TokenMissingNameException>(() =>
            {
                try
                {
                    parser.Parse(templateText);
                }
                catch (TokenMissingNameException ex)
                {
                    Assert.Equal("<%FOREACH %>", ex.Split.Content);
                    throw;
                }
            });
        }

        [Fact]
        public void ItShouldAbleToRespectRowTemplateElement()
        {
            string templateText = "<%FOREACH Name %>";
            templateText += "<%ROW%>Content within for<%ENDROW%>";
            templateText += "<%ENDFOR%>";

            var parser = new Parser();
            List<IToken> result = parser.Parse(templateText);

            Assert.Equal(1, result.Count);
            var token = (ForEachToken)result[0];
            Assert.Equal("name", token.Name);
            Assert.Equal(1, token.RowTokens.Count);

            var content = (ContentToken)token.RowTokens[0];
            Assert.Equal("Content within for", content.Content);
        }

        [Fact]
        public void ItShouldThrowExceptionWhenRowNotClosed1()
        {
            string templateText = "<%FOREACH Name %>";
            templateText += "<%ROW%>Content within for";
            templateText += "<%ENDFOR%>";

            var parser = new Parser();
            Assert.Throws<TokenNotClosedException>(() =>
            {
                try
                {
                    parser.Parse(templateText);
                }
                catch (TokenNotClosedException ex)
                {
                    Assert.Equal("name", ex.TokenName);
                    Assert.Equal("<%ROW%>", ex.Split.Content);
                    Assert.Equal("<%ROW%> not closed for <%FOREACH Name %>", ex.Message);
                    throw;
                }
            });
        }

        [Fact]
        public void ItShouldThrowExceptionWhenRowNotClosed2()
        {
            string templateText = "<%FOREACH Name %>";
            templateText += "<%ROW%>Content within for";

            var parser = new Parser();
            Assert.Throws<TokenNotClosedException>(() =>
            {
                try
                {
                    parser.Parse(templateText);
                }
                catch (TokenNotClosedException ex)
                {
                    Assert.Equal("name", ex.TokenName);
                    Assert.Equal("<%ROW%>", ex.Split.Content);
                    Assert.Equal("<%ROW%> not closed for <%FOREACH Name %>", ex.Message);
                    throw;
                }
            });

        }

        [Fact]
        public void ItShouldThrowErrorWhenClosingEndForMissing2()
        {
            string templateText = "<%FOREACH Name %>";
            templateText += "<%ROW%>Content within for<%ENDROW%>";

            var parser = new Parser();
            Assert.Throws<TokenNotClosedException>(() =>
            {
                try
                {
                    parser.Parse(templateText);
                }
                catch (TokenNotClosedException ex)
                {
                    Assert.Equal("name", ex.TokenName);
                    Assert.Equal("<%FOREACH Name %>", ex.Split.Content);
                    throw;
                }
            });

        }

        [Fact]
        public void ItShouldAbleToProcessVariousForInnerTokens()
        {
            string templateText = "<%FOREACH Name %>";

            templateText += "<%NORECORD%>Content within NORECORD<%ENDNORECORD%>";

            templateText += "<%HEADER%>Content within HEADER<%ENDHEADER%>";

            templateText += "<%BEFOREFIRSTROW%>Content within BEFOREFIRSTROW<%ENDBEFOREFIRSTROW%>";
            templateText += "<%FIRSTROW%>Content within FIRSTROW<%ENDFIRSTROW%>";
            templateText += "<%AFTERFIRSTROW%>Content within AFTERFIRSTROW<%ENDAFTERFIRSTROW%>";

            templateText += "<%BEFOREROW%>Content within BEFOREROW<%ENDBEFOREROW%>";
            templateText += "<%ROW%>Content within ROW<%ENDROW%>";
            templateText += "<%AFTERROW%>Content within AFTERROW<%ENDAFTERROW%>";

            templateText += "<%BEFOREALTROW%>Content within BEFOREALTROW<%ENDBEFOREALTROW%>";
            templateText += "<%ALTROW%>Content within ALTROW<%ENDALTROW%>";
            templateText += "<%AFTERALTROW%>Content within AFTERALTROW<%ENDAFTERALTROW%>";

            templateText += "<%BEFORELASTROW%>Content within BEFORELASTROW<%ENDBEFORELASTROW%>";
            templateText += "<%LASTROW%>Content within LASTROW<%ENDLASTROW%>";
            templateText += "<%AFTERLASTROW%>Content within AFTERLASTROW<%ENDAFTERLASTROW%>";

            templateText += "<%FOOTER%>Content within FOOTER<%ENDFOOTER%>";

            templateText += "<%ENDFOR%>";

            var parser = new Parser();
            List<IToken> result = parser.Parse(templateText);

            var assertMethod = new Action<List<IToken>, string>(
                (tokenList, innerContent) =>
                {
                    Assert.Equal(1, tokenList.Count);
                    var content = (ContentToken)tokenList[0];
                    Assert.Equal(innerContent, content.Content);
                });

            Assert.Equal(1, result.Count);
            var token = (ForEachToken)result[0];
            Assert.Equal("name", token.Name);

            assertMethod(token.NoRecordTokens, "Content within NORECORD");
            assertMethod(token.HeaderTokens, "Content within HEADER");

            assertMethod(token.BeforeFirstRowTokens, "Content within BEFOREFIRSTROW");
            assertMethod(token.FirstRowTokens, "Content within FIRSTROW");
            assertMethod(token.AfterFirstRowTokens, "Content within AFTERFIRSTROW");

            assertMethod(token.BeforeRowTokens, "Content within BEFOREROW");
            assertMethod(token.RowTokens, "Content within ROW");
            assertMethod(token.AfterRowTokens, "Content within AFTERROW");

            assertMethod(token.BeforeAltRowTokens, "Content within BEFOREALTROW");
            assertMethod(token.AltRowTokens, "Content within ALTROW");
            assertMethod(token.AfterAltRowTokens, "Content within AFTERALTROW");

            assertMethod(token.BeforeLastRowTokens, "Content within BEFORELASTROW");
            assertMethod(token.LastRowTokens, "Content within LASTROW");
            assertMethod(token.AfterLastRowTokens, "Content within AFTERLASTROW");

            assertMethod(token.FooterTokens, "Content within FOOTER");
        }

        [Fact]
        public void ItShouldThrowErrorForNotClosingVariousForInnerTokens()
        {
            const string templateBegin = "<%FOREACH Name %>";
            const string templateEnd = "<%ENDFOR%>";

            var assertMethod = new Action<string>(
                (string token) =>
                {
                    string templateText = templateBegin + "<%" + token + "%>Content within " + token + templateEnd;
                    var parser = new Parser();
                    try
                    {
                        parser.Parse(templateText);
                    }
                    catch (TokenNotClosedException ex)
                    {
                        Assert.Equal("name", ex.TokenName);
                        Assert.Equal("<%" + token + "%>", ex.Split.Content);
                        Assert.Equal("<%" + token + "%> not closed for <%FOREACH Name %>", ex.Message);
                        return;
                    }

                    Assert.True(false); //this is to ensure try catch actually ran.
                });

            assertMethod("NORECORD");
            assertMethod("HEADER");

            assertMethod("BEFOREFIRSTROW");
            assertMethod("FIRSTROW");
            assertMethod("AFTERFIRSTROW");

            assertMethod("BEFOREROW");
            assertMethod("ROW");
            assertMethod("AFTERROW");

            assertMethod("BEFOREALTROW");
            assertMethod("ALTROW");
            assertMethod("AFTERALTROW");

            assertMethod("BEFORELASTROW");
            assertMethod("LASTROW");
            assertMethod("AFTERLASTROW");

            assertMethod("FOOTER");
        }

        [Fact]
        public void ItShouldThrowExceptionWithInvalidToken1()
        {
            string templateText = "<%FOREACH Name %>";

            templateText += "<%NORECORD%>Content within NORECORD<%ENDNORECORD%>";

            templateText += "<%HEADER%>Content within HEADER<%ENDHEADER%>";

            templateText += "<%BEFOREFIRSTROW%>Content within BEFOREFIRSTROW<%ENDBEFOREFIRSTROW%>";
            templateText += "<%FIRSTROW%>Content within FIRSTROW<%ENDFIRSTROW%>";
            templateText += "<%AFTERFIRSTROW%>Content within AFTERFIRSTROW<%ENDAFTERFIRSTROW%>";

            templateText += "<%BEFOREROW%>Content within BEFOREROW<%ENDBEFOREROW%>";
            templateText += "<%ROW%>Content within ROW<%ENDROW%>";
            templateText += "<%AFTERROW%>Content within AFTERROW<%ENDAFTERROW%>";

            templateText += "<%BEFOREALTROW%>Content within BEFOREALTROW<%ENDBEFOREALTROW%>";
            templateText += "<%ALTROW%>Content within ALTROW<%ENDALTROW%>";
            templateText += "<%AFTERALTROW%>Content within AFTERALTROW<%ENDAFTERALTROW%>";

            templateText += "<%BEFORELASTROW%>Content within BEFORELASTROW<%ENDBEFORELASTROW%>";
            templateText += "<%LASTROW%>Content within LASTROW<%ENDLASTROW%>";
            templateText += "<%AFTERLASTROW%>Content within AFTERLASTROW<%ENDAFTERLASTROW%>";

            templateText += "<%FOOTER%>Content within FOOTER<%ENDFOOTER%>";

            templateText += "<%FOOTER %>Content within FOOTER<%ENDFOOTER%>";

            templateText += "<%ENDFOR%>";

            var parser = new Parser();
            Assert.Throws<ParserException>(() =>
            {
                parser.Parse(templateText);
            });

        }

        [Fact]
        public void ItShouldThrowExceptionWithInvalidToken2()
        {
            string templateText = "<%FOREACH Name %>";

            templateText += "<%NORECORD %>Content within NORECORD<%ENDNORECORD%>";

            templateText += "<%FOOTER%>Content within FOOTER<%ENDFOOTER%>";

            templateText += "<%ENDFOR%>";

            var parser = new Parser();
            Assert.Throws<ParserException>(() =>
            {
                parser.Parse(templateText);
            });
        }

        [Fact]
        public void ItShouldThrowExceptionWithInvalidToken3()
        {
            string templateText = "<%FOREACH Name %>";

            templateText += "<%NORECORD%>Content within NORECORD<%ENDNORECORD%>";
            templateText += "<%NORECORD%>Content within NORECORD<%ENDNORECORD%>"; //duplicate token will throw error.
            templateText += "<%ENDFOR%>";

            var parser = new Parser();
            Assert.Throws<ParserException>(() =>
            {
                parser.Parse(templateText);
            });
        }

        [Fact]
        public void ItShouldProcessLastRowTokenWhenThereIsOnlyOneRowForLoop()
        {
            string templateText = "<%FOREACH Name %>";

            templateText += "<%HEADER%>Content within HEADER<%ENDHEADER%>";

            templateText += "<%BEFOREFIRSTROW%>Content within BEFOREFIRSTROW<%ENDBEFOREFIRSTROW%>";
            templateText += "<%FIRSTROW%>Content within FIRSTROW<%ENDFIRSTROW%>";
            templateText += "<%AFTERFIRSTROW%>Content within AFTERFIRSTROW<%ENDAFTERFIRSTROW%>";

            templateText += "<%BEFOREROW%>Content within BEFOREROW<%ENDBEFOREROW%>";
            templateText += "<%ROW%>Content within ROW<%ENDROW%>";
            templateText += "<%AFTERROW%>Content within AFTERROW<%ENDAFTERROW%>";

            templateText += "<%BEFOREALTROW%>Content within BEFOREALTROW<%ENDBEFOREALTROW%>";
            templateText += "<%ALTROW%>Content within ALTROW<%ENDALTROW%>";
            templateText += "<%AFTERALTROW%>Content within AFTERALTROW<%ENDAFTERALTROW%>";

            templateText += "<%BEFORELASTROW%>Content within BEFORELASTROW<%ENDBEFORELASTROW%>";
            templateText += "<%LASTROW%>Content within LASTROW<%ENDLASTROW%>";
            templateText += "<%AFTERLASTROW%>Content within AFTERLASTROW<%ENDAFTERLASTROW%>";

            templateText += "<%FOOTER%>Content within FOOTER<%ENDFOOTER%>";

            templateText += "<%ENDFOR%>";

            var parser = new Parser();
            List<IToken> result = parser.Parse(templateText);

            var assertMethod = new Action<List<IToken>, string>(
                (tokenList, innerContent) =>
                {
                    Assert.Equal(1, tokenList.Count);
                    var content = (ContentToken)tokenList[0];
                    Assert.Equal(innerContent, content.Content);
                });

            Assert.Equal(1, result.Count);
            var token = (ForEachToken)result[0];
            Assert.Equal("name", token.Name);

            assertMethod(token.HeaderTokens, "Content within HEADER");

            assertMethod(token.BeforeFirstRowTokens, "Content within BEFOREFIRSTROW");
            assertMethod(token.FirstRowTokens, "Content within FIRSTROW");
            assertMethod(token.AfterFirstRowTokens, "Content within AFTERFIRSTROW");

            assertMethod(token.BeforeRowTokens, "Content within BEFOREROW");
            assertMethod(token.RowTokens, "Content within ROW");
            assertMethod(token.AfterRowTokens, "Content within AFTERROW");

            assertMethod(token.BeforeAltRowTokens, "Content within BEFOREALTROW");
            assertMethod(token.AltRowTokens, "Content within ALTROW");
            assertMethod(token.AfterAltRowTokens, "Content within AFTERALTROW");

            assertMethod(token.BeforeLastRowTokens, "Content within BEFORELASTROW");
            assertMethod(token.LastRowTokens, "Content within LASTROW");
            assertMethod(token.AfterLastRowTokens, "Content within AFTERLASTROW");

            assertMethod(token.FooterTokens, "Content within FOOTER");
        }

    }
}