// Title: Provider classes with the Signatures interfaces
// Summary: The Processor takes delegates. When the providers belong together, put them in one class that implements the interfaces of Toshal.Template.Signatures, then assign its methods.

using System.Collections;
using Toshal.Template.Examples.Support;
using Toshal.Template.Signatures;
using Toshal.Template.Tokens;

namespace Toshal.Template.Examples;

public static class ProviderInterfacesExample
{
    // One class answers every kind of tag for a newsletter.
    private sealed class NewsletterProvider : ITokenValueProvider, IConditionValueProvider, ILoopValueProvider, IWithValueProvider, IProcessTemplateValueProvider
    {
        private readonly List<IToken> footer = new Parser().Parse("Unsubscribe any time.");

        public string TokenValueProvider(TokenArgs args) => args.Name == "title" ? args.Context?.ToString() ?? "" : "";

        public bool ConditionValueProvider(ConditionArgs args) => args.Name == "hasnews";

        public IList LoopValueProvider(LoopArgs args) => new[] { "Release 1.2", "New examples" };

        public object WithValueProvider(TokenArgs args) => "Toshal news";

        public List<IToken> ProcessTemplateValueProvider(ProcessTemplateArgs args) => footer;
    }

    public static void Run()
    {
        Console.WriteLine("== Provider interfaces ==");

        var provider = new NewsletterProvider();
        var processor = new Processor
        {
            TokenValueProvider = provider.TokenValueProvider,
            ConditionValueProvider = provider.ConditionValueProvider,
            LoopValueProvider = provider.LoopValueProvider,
            WithValueProvider = provider.WithValueProvider,
            ProcessTemplateValueProvider = provider.ProcessTemplateValueProvider,
        };

        const string template = "<%WITH paper%><%=Title%><%ENDWITH%>: <%IF HasNews%><%FOREACH items%><%CONTEXT_AS_STRING%>; <%ENDFOR%><%ENDIF%><%PROCESS_TEMPLATE footer%>";
        string text = processor.Process(new ProcessorArgs(new Parser().Parse(template))).ToString();
        Console.WriteLine(text);
        Verify.Equal("Toshal news: Release 1.2; New examples; Unsubscribe any time.", text, "interfaces");
    }
}
