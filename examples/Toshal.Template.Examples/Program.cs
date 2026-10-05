// Title: Run all examples
// Summary: The entry point. It runs every topic in order and needs no database. The database half of the pattern example runs only when the connection string is set.

using Toshal.Template.Examples;

// To run the database half of the pattern example, point it to an empty PostgreSQL database, for example:
//   set ConnectionStrings__testdb=Host=localhost;Database=testdb;Username=postgres;Password=secret
string? connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__testdb");

QuickStartExample.Run();
ValuesAndAttributesExample.Run();
ConditionsExample.Run();
LoopsExample.Run();
ReuseForEachExample.Run();
WithAndParentContextExample.Run();
SetVariablesExample.Run();
WhitespaceExample.Run();
SubTemplatesExample.Run();
ParserErrorsExample.Run();
TokenTreeExample.Run();
ProviderInterfacesExample.Run();
EmailExample.Run();
ReportExample.Run();
CodeGenerationExample.Run();
ProjectStructureExample.Run();
await FakeOrRealTestPatternExample.Run(connectionString);

Console.WriteLine();
Console.WriteLine("All examples finished.");
