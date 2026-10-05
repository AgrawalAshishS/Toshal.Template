// Title: Inspect the parsed tokens
// Summary: Parser.Parse returns a tree of tokens with their line and column. Walk it to lint templates, list the names a template needs, or unit test a provider on its own with hand made args.

using System.Collections;
using Toshal.Template.Examples.Support;
using Toshal.Template.Tokens;

namespace Toshal.Template.Examples;

public static class TokenTreeExample
{
    public static void Run()
    {
        Console.WriteLine("== Token tree ==");

        const string template =
            "Hi <%=Name%>\n" +
            "<%IF not Paid THEN%>Pay now<%ELSEIF Partly%>Pay the rest<%ELSE%>Thanks<%ENDIF%>\n" +
            "<%FOREACH Lines%><%HEADER%>Lines:<%ENDHEADER%><%=Item%><%ENDFOR%>\n" +
            "<%REUSE_FOREACH Lines Returns%><%WITH Customer%><%SET x%>1<%ENDSET%><%ENDWITH%>" +
            "<%PROCESS_TEMPLATE Footer%><%REMOVE_PREVIOUS 1%><%REMOVE_PREVIOUS_NEW_LINE%><%CONTEXT_AS_STRING%>";
        List<IToken> tokens = new Parser().Parse(template);

        // List every name the template needs, with its position.
        var names = new List<string>();
        void Walk(IEnumerable<IToken> list)
        {
            foreach (IToken token in list)
            {
                switch (token)
                {
                    case NamedToken n: names.Add($"value {n.Name} @{n.LineNumber}:{n.StartingPosition}"); break;
                    case ConditionToken c:
                        names.Add($"condition {(c.IsPositive ? "" : "not ")}{c.Name} @{c.LineNumber}:{c.StartingPosition}");
                        Walk(c.InnerTokens);
                        if (c.FalsePart is ConditionToken elseIf) Walk([elseIf]);
                        else if (c.FalsePart is ElseToken e) Walk(e.InnerTokens);
                        break;
                    case ForEachToken f:
                        names.Add($"loop {f.Name} @{f.LineNumber}:{f.StartingPosition}");
                        Walk(f.HeaderTokens.Concat(f.RowTokens).Concat(f.FooterTokens).Concat(f.NoRecordTokens));
                        break;
                    case ReuseForEachToken r: names.Add($"loop {r.Name} (layout of {r.ExistingForEachName}, linked: {r.ExistingForEachToken != null})"); break;
                    case WithToken w: names.Add($"with {w.Name}"); Walk(w.InnerTokens); break;
                    case SetToken s: names.Add($"variable {s.Name}"); break;
                    case ProcessTemplateToken p: names.Add($"sub template {p.Name}"); break;
                    case RemovePreviousCharsToken rc: names.Add($"remove {rc.CharCount} chars"); break;
                }
            }
        }
        Walk(tokens);
        names.ForEach(n => Console.WriteLine("  " + n));
        Verify.That(names.Contains("value name @1:4") && names.Contains("condition not paid @2:1") && names.Contains("loop lines @3:1"), "names and positions");
        Verify.That(names.Contains("loop returns (layout of lines, linked: True)"), "reuse is linked");

        // Every token type, by position in the list.
        Verify.That(tokens[0] is ContentToken { Content: "Hi " }, "ContentToken");
        Verify.That(tokens.OfType<RemovePreviousNewLineToken>().Count() == 1 && tokens.OfType<ContextAsStringToken>().Count() == 1, "other tokens");
        Verify.That(tokens.OfType<IContainerToken>().Count() == 2 && tokens.OfType<ContainerTokenBase>().All(t => t.InnerTokens != null), "containers");
        Verify.That(tokens.OfType<Token>().Count() == tokens.Count, "all tokens derive from Token");

        // A FOREACH has one token list per part. Every tag that takes attributes exposes them.
        var parts = (ForEachToken)new Parser().Parse(
            "<%FOREACH rows by=\"id\"%><%NORECORD%>0<%ENDNORECORD%><%HEADER%>h<%ENDHEADER%><%BEFOREFIRSTROW%>1<%ENDBEFOREFIRSTROW%><%FIRSTROW%>2<%ENDFIRSTROW%>" +
            "<%AFTERFIRSTROW%>3<%ENDAFTERFIRSTROW%><%BEFOREROW%>4<%ENDBEFOREROW%><%ROW%>5<%ENDROW%><%AFTERROW%>6<%ENDAFTERROW%><%BEFOREALTROW%>7<%ENDBEFOREALTROW%>" +
            "<%ALTROW%>8<%ENDALTROW%><%AFTERALTROW%>9<%ENDAFTERALTROW%><%BEFORELASTROW%>a<%ENDBEFORELASTROW%><%LASTROW%>b<%ENDLASTROW%><%AFTERLASTROW%>c<%ENDAFTERLASTROW%>" +
            "<%FOOTER%>f<%ENDFOOTER%><%ENDFOR%>")[0];
        var allParts = new[]
        {
            parts.NoRecordTokens, parts.HeaderTokens, parts.BeforeFirstRowTokens, parts.FirstRowTokens, parts.AfterFirstRowTokens,
            parts.BeforeRowTokens, parts.RowTokens, parts.AfterRowTokens, parts.BeforeAltRowTokens, parts.AltRowTokens, parts.AfterAltRowTokens,
            parts.BeforeLastRowTokens, parts.LastRowTokens, parts.AfterLastRowTokens, parts.FooterTokens,
        };
        Verify.That(allParts.All(p => p.Count == 1) && parts.Attributes.GetValue("by", "") == "id", "every FOREACH part");

        var tagged = new ProcessorArgs(new Parser().Parse(
            "<%IF a x=\"1\"%><%ENDIF%><%WITH b y=\"2\"%><%ENDWITH%><%SET c z=\"3\"%><%ENDSET%><%PROCESS_TEMPLATE d lang=\"en\"%>"));
        var sub2 = (ProcessTemplateToken)tagged.TokenList[3];
        Verify.That(((ConditionToken)tagged.TokenList[0]).Attributes["x"] == "1" && ((WithToken)tagged.TokenList[1]).Attributes["y"] == "2"
            && ((SetToken)tagged.TokenList[2]).Attributes["z"] == "3" && sub2.Attributes["lang"] == "en" && sub2.GetAttribute("LANG", "") == "en", "attributes of every tag");

        string? langSeen = null;
        new Processor { ProcessTemplateValueProvider = a => { langSeen = a.Attributes.GetValue("lang", ""); return null; } }.Process(tagged);
        Verify.That(langSeen == "en", "ProcessTemplateArgs.Attributes");

        Token first = (Token)tagged.TokenList[0];
        Verify.That(first.LineNumber == 1 && first.StartingPosition == 1, "Token position");

        // Tokens can be made by hand from a Split, for example to unit test a provider without a template.
        var hand = new List<IToken>
        {
            new ContentToken(new Split { Content = "x" }),
            new ContextAsStringToken(new Split { Content = "<%CONTEXT_AS_STRING%>" }),
            new RemovePreviousNewLineToken(new Split { Content = "<%REMOVE_PREVIOUS_NEW_LINE%>" }),
            new RemovePreviousCharsToken(new Split { Content = "<%REMOVE_PREVIOUS 1%>" }),
            new ElseToken(new Split { Content = "<%ELSE%>" }),
            new SetToken(new Split { Content = "<%SET v%>" }),
            new WithToken(new Split { Content = "<%WITH c%>" }),
            new ReuseForEachToken(new Split { Content = "<%REUSE_FOREACH a b%>" }),
        };
        Verify.That(hand.Count == 8, "hand made tokens");

        // Test a provider alone: build the args the processor would build.
        Func<ConditionArgs, bool> conditions = a => a.Name == "paid" && (bool)a.Context!;
        Func<LoopArgs, IList?> loops = a => a.Name == "lines" ? new[] { 1, 2 } : null;
        Func<TokenArgs, string?> values = a => a.Name == "name" ? "Asha" : null;
        Func<TokenArgs, object?> withs = a => a.Name == "customer" ? "c" : null;
        Func<ProcessTemplateArgs, List<IToken>?> subs = a => a.Name == "footer" ? [] : null;

        var cond = new ConditionToken(new Split { Content = "<%IF Paid%>" });
        var loop = new ForEachToken(new Split { Content = "<%FOREACH Lines%>" });
        var value = new NamedToken(new Split { Content = "<%=Name%>" });
        var with = new WithToken(new Split { Content = "<%WITH Customer%>" });
        var sub = new ProcessTemplateToken(new Split { Content = "<%PROCESS_TEMPLATE Footer%>" });
        var none = new List<object?>();

        Verify.That(conditions(new ConditionArgs(cond, true, none)), "ConditionArgs");
        Verify.That(loops(new LoopArgs(loop, null, none))!.Count == 2, "LoopArgs");
        Verify.That(values(new TokenArgs(value, null, none)) == "Asha", "TokenArgs for a value");
        Verify.That(withs(new TokenArgs(with, null, none)) is "c", "TokenArgs for WITH");
        Verify.That(subs(new ProcessTemplateArgs(sub, null, none)) != null, "ProcessTemplateArgs");
        Console.WriteLine("Providers tested alone with hand made args.");
    }
}
