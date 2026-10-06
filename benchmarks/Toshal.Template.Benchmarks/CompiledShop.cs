// The values of CompiledShop.ctt: the same providers as the Floor benchmark, as partial methods of the compiled class.

#nullable enable

namespace Toshal.Template.Benchmarks
{
    using System.Collections;

    using Toshal.Template;

    public partial class CompiledShop
    {
        private partial string? TokenValue(TokenArgs args) => Globals.Token(args) ?? args.Context switch
        {
            Node<T29> => Custom<T29>.Token(args) ?? Generated<T29>.Token(args),
            Node<T15> => Generated<T15>.Token(args),
            _ => null,
        };

        private partial bool Condition(ConditionArgs args) => Custom<T29>.Condition(args) || Generated<T29>.Condition(args);

        private partial IList? Loop(LoopArgs args) => args.Context is Node<T00> ? Generated<T00>.Loop(args) : Generated<T15>.Loop(args);

        private partial object? With(TokenArgs args) => Generated<T29>.With(args);
    }
}
