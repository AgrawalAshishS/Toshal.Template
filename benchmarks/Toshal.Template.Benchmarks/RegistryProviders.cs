// The same answers as Custom<TTag>, Generated<TTag> and Globals, written for ContextProviderRegistry.

using System.Collections;
using Toshal.Template.Providers;

namespace Toshal.Template.Benchmarks;

public sealed class CustomProvider<TTag> : ContextProvider<Node<TTag>> where TTag : struct
{
    public override bool TryToken(Node<TTag> n, TokenArgs a, out string? value)
    {
        switch (a.Name)
        {
            case "nullable_type_name": value = n.IsNullable ? n.TypeName + "?" : n.TypeName; return true;
        }

        value = null;
        return false;
    }

    public override bool TryCondition(Node<TTag> n, ConditionArgs a, out bool value)
    {
        switch (a.Name)
        {
            case "is_string": value = n.TypeName == "string"; return true;
        }

        value = false;
        return false;
    }
}

public sealed class GeneratedProvider<TTag> : ContextProvider<Node<TTag>> where TTag : struct
{
    public override bool TryToken(Node<TTag> n, TokenArgs a, out string? value)
    {
        switch (a.Name)
        {
            case "name": value = n.Name; return true;
            case "typename":
            case "type_name": value = n.TypeName; return true;
            case "isnullable":
            case "is_nullable": value = n.IsNullable.ToString(); return true;
        }

        value = null;
        return false;
    }

    public override bool TryCondition(Node<TTag> n, ConditionArgs a, out bool value)
    {
        switch (a.Name)
        {
            case "name": value = n.Name.Length > 0; return true;
            case "isnullable":
            case "is_nullable": value = n.IsNullable; return true;
            case "items": value = n.Items.Count > 0; return true;
        }

        value = false;
        return false;
    }

    public override bool TryLoop(Node<TTag> n, LoopArgs a, out IList? value)
    {
        switch (a.Name)
        {
            case "items": value = n.Items; return true;
        }

        value = null;
        return false;
    }

    public override bool TryWith(Node<TTag> n, TokenArgs a, out object? value)
    {
        switch (a.Name)
        {
            case "parent": value = n.Parent; return true;
        }

        value = null;
        return false;
    }
}

public sealed class GlobalNames : GlobalProvider
{
    public override bool TryToken(TokenArgs a, out string? value)
    {
        value = Globals.Token(a);
        return value != null;
    }
}

public static class RegistrySetup
{
    // All 30 types, Custom then Generated, like the chain.
    public static ContextProviderRegistry Build()
    {
        var r = new ContextProviderRegistry().RegisterGlobal(new GlobalNames(), GlobalOrder.BeforeTyped);
        Add<T00>(r); Add<T01>(r); Add<T02>(r); Add<T03>(r); Add<T04>(r); Add<T05>(r); Add<T06>(r); Add<T07>(r); Add<T08>(r); Add<T09>(r);
        Add<T10>(r); Add<T11>(r); Add<T12>(r); Add<T13>(r); Add<T14>(r); Add<T15>(r); Add<T16>(r); Add<T17>(r); Add<T18>(r); Add<T19>(r);
        Add<T20>(r); Add<T21>(r); Add<T22>(r); Add<T23>(r); Add<T24>(r); Add<T25>(r); Add<T26>(r); Add<T27>(r); Add<T28>(r); Add<T29>(r);
        return r;
    }

    private static void Add<TTag>(ContextProviderRegistry r) where TTag : struct
    {
        r.Register(new CustomProvider<TTag>()).Register(new GeneratedProvider<TTag>());
    }
}
