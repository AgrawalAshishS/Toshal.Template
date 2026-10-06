using System;
using System.Threading.Tasks;

using Toshal.Template.Examples;
using Xunit;

namespace Toshal.Template.Tests.Examples
{
    // Every example checks its own output and throws when it is wrong. Running them here keeps the docs and the examples honest.
    public class ExamplesTests
    {
        [Fact] public void QuickStart() => QuickStartExample.Run();
        [Fact] public void ValuesAndAttributes() => ValuesAndAttributesExample.Run();
        [Fact] public void Conditions() => ConditionsExample.Run();
        [Fact] public void Loops() => LoopsExample.Run();
        [Fact] public void ReuseForEach() => ReuseForEachExample.Run();
        [Fact] public void WithAndParentContext() => WithAndParentContextExample.Run();
        [Fact] public void SetVariables() => SetVariablesExample.Run();
        [Fact] public void Whitespace() => WhitespaceExample.Run();
        [Fact] public void SubTemplates() => SubTemplatesExample.Run();
        [Fact] public void ParserErrors() => ParserErrorsExample.Run();
        [Fact] public void TokenTree() => TokenTreeExample.Run();
        [Fact] public void ProviderInterfaces() => ProviderInterfacesExample.Run();
        [Fact] public void ContextProviders() => ContextProvidersExample.Run();
        [Fact] public void Email() => EmailExample.Run();
        [Fact] public void Report() => ReportExample.Run();
        [Fact] public void CodeGeneration() => CodeGenerationExample.Run();
        [Fact] public void CompiledTemplates() => CompiledTemplatesExample.Run();
        [Fact] public void ProjectStructure() => ProjectStructureExample.Run();

        [Fact]
        public Task PatternWithFakes() => FakeOrRealTestPatternExample.ConfirmationEmails(TestMode.Fake);

        // Runs only when the environment variable ConnectionStrings__testdb points to an empty PostgreSQL database.
        [DatabaseFact]
        public Task PatternWithDatabase() => FakeOrRealTestPatternExample.ConfirmationEmails(TestMode.Database, Environment.GetEnvironmentVariable(DatabaseFactAttribute.Variable));
    }

    /// <summary>A fact that is skipped when no test database is set, so the test run stays offline by default.</summary>
    public sealed class DatabaseFactAttribute : FactAttribute
    {
        public const string Variable = "ConnectionStrings__testdb";

        public DatabaseFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(Variable)))
            {
                Skip = $"Set the environment variable {Variable} to run the database half of the pattern.";
            }
        }
    }
}
