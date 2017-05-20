using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Test.Template
{
    using Toshal.Template;
    using Toshal.Template.Exceptions;
    using Toshal.Template.Tokens;

    [TestFixture]
    public class ForEachParserTests
    {
        //TODO: Add For each tests for multiple occurrence of inner elements.

        [Test]
        public void ItShouldHandleBasicForEach()
        {
            string templateText = "<%FOREACH Name %>";
            templateText += "Content within for";
            templateText += "<%ENDFOR%>";

            var parser = new Parser();
            List<IToken> result = parser.Parse(templateText);

            Assert.AreEqual(1, result.Count);
            var token = (ForEachToken)result[0];
            Assert.AreEqual("name", token.Name);
            Assert.AreEqual(1, token.RowTokens.Count);

            var content = (ContentToken)token.RowTokens[0];
            Assert.AreEqual("Content within for", content.Content);
        }

        [Test]
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
                    Assert.AreEqual("name", ex.TokenName);
                    Assert.AreEqual("<%FOREACH Name %>", ex.Split);
                    throw;
                }
            });
        }

        [Test]
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
                    Assert.AreEqual("<%FOREACH %>", ex.Split);
                    throw;
                }
            });
        }

        [Test]
        public void ItShouldAbleToRespectRowTemplateElement()
        {
            string templateText = "<%FOREACH Name %>";
            templateText += "<%ROW%>Content within for<%ENDROW%>";
            templateText += "<%ENDFOR%>";

            var parser = new Parser();
            List<IToken> result = parser.Parse(templateText);

            Assert.AreEqual(1, result.Count);
            var token = (ForEachToken)result[0];
            Assert.AreEqual("name", token.Name);
            Assert.AreEqual(1, token.RowTokens.Count);

            var content = (ContentToken)token.RowTokens[0];
            Assert.AreEqual("Content within for", content.Content);
        }

        [Test]
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
                    Assert.AreEqual("name", ex.TokenName);
                    Assert.AreEqual("<%FOREACH Name %>", ex.Split);
                    Assert.AreEqual("<%ROW%> not closed for <%FOREACH Name %>", ex.Message);
                    throw;
                }
            });
        }

        [Test]
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
                    Assert.AreEqual("name", ex.TokenName);
                    Assert.AreEqual("<%FOREACH Name %>", ex.Split);
                    Assert.AreEqual("<%ROW%> not closed for <%FOREACH Name %>", ex.Message);
                    throw;
                }
            });

        }

        [Test]
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
                    Assert.AreEqual("name", ex.TokenName);
                    Assert.AreEqual("<%FOREACH Name %>", ex.Split);
                    throw;
                }
            });

        }

        [Test]
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
                    Assert.AreEqual(1, tokenList.Count);
                    var content = (ContentToken)tokenList[0];
                    Assert.AreEqual(innerContent, content.Content);
                });

            Assert.AreEqual(1, result.Count);
            var token = (ForEachToken)result[0];
            Assert.AreEqual("name", token.Name);

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

        [Test]
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
                        Assert.AreEqual("name", ex.TokenName);
                        Assert.AreEqual("<%FOREACH Name %>", ex.Split);
                        Assert.AreEqual("<%" + token + "%> not closed for <%FOREACH Name %>", ex.Message);
                        return;
                    }

                    Assert.IsTrue(false); //this is to ensure try catch actually ran.
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

        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
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
                    Assert.AreEqual(1, tokenList.Count);
                    var content = (ContentToken)tokenList[0];
                    Assert.AreEqual(innerContent, content.Content);
                });

            Assert.AreEqual(1, result.Count);
            var token = (ForEachToken)result[0];
            Assert.AreEqual("name", token.Name);

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