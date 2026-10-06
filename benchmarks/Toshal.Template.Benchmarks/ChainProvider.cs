// Made by a script: the chain style of a consumer with 30 context types, Custom then Generated for each.
// It copies the shape of a real code generator: four methods, each tries 60 providers in a fixed order.

using System.Collections;

namespace Toshal.Template.Benchmarks;

public static class ChainProvider
{
    public static string? Token(TokenArgs a)
    {
        var g = Globals.Token(a); if (g != null) return g;
        if (a.Context == null) return null;
        var r = Custom<T00>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Generated<T00>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Custom<T01>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Generated<T01>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Custom<T02>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Generated<T02>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Custom<T03>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Generated<T03>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Custom<T04>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Generated<T04>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Custom<T05>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Generated<T05>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Custom<T06>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Generated<T06>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Custom<T07>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Generated<T07>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Custom<T08>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Generated<T08>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Custom<T09>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Generated<T09>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Custom<T10>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Generated<T10>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Custom<T11>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Generated<T11>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Custom<T12>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Generated<T12>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Custom<T13>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Generated<T13>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Custom<T14>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Generated<T14>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Custom<T15>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Generated<T15>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Custom<T16>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Generated<T16>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Custom<T17>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Generated<T17>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Custom<T18>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Generated<T18>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Custom<T19>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Generated<T19>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Custom<T20>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Generated<T20>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Custom<T21>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Generated<T21>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Custom<T22>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Generated<T22>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Custom<T23>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Generated<T23>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Custom<T24>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Generated<T24>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Custom<T25>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Generated<T25>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Custom<T26>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Generated<T26>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Custom<T27>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Generated<T27>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Custom<T28>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Generated<T28>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Custom<T29>.Token(a);
        if (string.IsNullOrEmpty(r)) r = Generated<T29>.Token(a);
        return r;
    }

    public static bool Condition(ConditionArgs a)
    {
        if (a.Context == null) return false;
        var r = Custom<T00>.Condition(a);
        if (r == false) r = Generated<T00>.Condition(a);
        if (r == false) r = Custom<T01>.Condition(a);
        if (r == false) r = Generated<T01>.Condition(a);
        if (r == false) r = Custom<T02>.Condition(a);
        if (r == false) r = Generated<T02>.Condition(a);
        if (r == false) r = Custom<T03>.Condition(a);
        if (r == false) r = Generated<T03>.Condition(a);
        if (r == false) r = Custom<T04>.Condition(a);
        if (r == false) r = Generated<T04>.Condition(a);
        if (r == false) r = Custom<T05>.Condition(a);
        if (r == false) r = Generated<T05>.Condition(a);
        if (r == false) r = Custom<T06>.Condition(a);
        if (r == false) r = Generated<T06>.Condition(a);
        if (r == false) r = Custom<T07>.Condition(a);
        if (r == false) r = Generated<T07>.Condition(a);
        if (r == false) r = Custom<T08>.Condition(a);
        if (r == false) r = Generated<T08>.Condition(a);
        if (r == false) r = Custom<T09>.Condition(a);
        if (r == false) r = Generated<T09>.Condition(a);
        if (r == false) r = Custom<T10>.Condition(a);
        if (r == false) r = Generated<T10>.Condition(a);
        if (r == false) r = Custom<T11>.Condition(a);
        if (r == false) r = Generated<T11>.Condition(a);
        if (r == false) r = Custom<T12>.Condition(a);
        if (r == false) r = Generated<T12>.Condition(a);
        if (r == false) r = Custom<T13>.Condition(a);
        if (r == false) r = Generated<T13>.Condition(a);
        if (r == false) r = Custom<T14>.Condition(a);
        if (r == false) r = Generated<T14>.Condition(a);
        if (r == false) r = Custom<T15>.Condition(a);
        if (r == false) r = Generated<T15>.Condition(a);
        if (r == false) r = Custom<T16>.Condition(a);
        if (r == false) r = Generated<T16>.Condition(a);
        if (r == false) r = Custom<T17>.Condition(a);
        if (r == false) r = Generated<T17>.Condition(a);
        if (r == false) r = Custom<T18>.Condition(a);
        if (r == false) r = Generated<T18>.Condition(a);
        if (r == false) r = Custom<T19>.Condition(a);
        if (r == false) r = Generated<T19>.Condition(a);
        if (r == false) r = Custom<T20>.Condition(a);
        if (r == false) r = Generated<T20>.Condition(a);
        if (r == false) r = Custom<T21>.Condition(a);
        if (r == false) r = Generated<T21>.Condition(a);
        if (r == false) r = Custom<T22>.Condition(a);
        if (r == false) r = Generated<T22>.Condition(a);
        if (r == false) r = Custom<T23>.Condition(a);
        if (r == false) r = Generated<T23>.Condition(a);
        if (r == false) r = Custom<T24>.Condition(a);
        if (r == false) r = Generated<T24>.Condition(a);
        if (r == false) r = Custom<T25>.Condition(a);
        if (r == false) r = Generated<T25>.Condition(a);
        if (r == false) r = Custom<T26>.Condition(a);
        if (r == false) r = Generated<T26>.Condition(a);
        if (r == false) r = Custom<T27>.Condition(a);
        if (r == false) r = Generated<T27>.Condition(a);
        if (r == false) r = Custom<T28>.Condition(a);
        if (r == false) r = Generated<T28>.Condition(a);
        if (r == false) r = Custom<T29>.Condition(a);
        if (r == false) r = Generated<T29>.Condition(a);
        return r;
    }

    public static IList? Loop(LoopArgs a)
    {
        if (a.Context == null) return null;
        var r = Custom<T00>.Loop(a);
        if (r == null) r = Generated<T00>.Loop(a);
        if (r == null) r = Custom<T01>.Loop(a);
        if (r == null) r = Generated<T01>.Loop(a);
        if (r == null) r = Custom<T02>.Loop(a);
        if (r == null) r = Generated<T02>.Loop(a);
        if (r == null) r = Custom<T03>.Loop(a);
        if (r == null) r = Generated<T03>.Loop(a);
        if (r == null) r = Custom<T04>.Loop(a);
        if (r == null) r = Generated<T04>.Loop(a);
        if (r == null) r = Custom<T05>.Loop(a);
        if (r == null) r = Generated<T05>.Loop(a);
        if (r == null) r = Custom<T06>.Loop(a);
        if (r == null) r = Generated<T06>.Loop(a);
        if (r == null) r = Custom<T07>.Loop(a);
        if (r == null) r = Generated<T07>.Loop(a);
        if (r == null) r = Custom<T08>.Loop(a);
        if (r == null) r = Generated<T08>.Loop(a);
        if (r == null) r = Custom<T09>.Loop(a);
        if (r == null) r = Generated<T09>.Loop(a);
        if (r == null) r = Custom<T10>.Loop(a);
        if (r == null) r = Generated<T10>.Loop(a);
        if (r == null) r = Custom<T11>.Loop(a);
        if (r == null) r = Generated<T11>.Loop(a);
        if (r == null) r = Custom<T12>.Loop(a);
        if (r == null) r = Generated<T12>.Loop(a);
        if (r == null) r = Custom<T13>.Loop(a);
        if (r == null) r = Generated<T13>.Loop(a);
        if (r == null) r = Custom<T14>.Loop(a);
        if (r == null) r = Generated<T14>.Loop(a);
        if (r == null) r = Custom<T15>.Loop(a);
        if (r == null) r = Generated<T15>.Loop(a);
        if (r == null) r = Custom<T16>.Loop(a);
        if (r == null) r = Generated<T16>.Loop(a);
        if (r == null) r = Custom<T17>.Loop(a);
        if (r == null) r = Generated<T17>.Loop(a);
        if (r == null) r = Custom<T18>.Loop(a);
        if (r == null) r = Generated<T18>.Loop(a);
        if (r == null) r = Custom<T19>.Loop(a);
        if (r == null) r = Generated<T19>.Loop(a);
        if (r == null) r = Custom<T20>.Loop(a);
        if (r == null) r = Generated<T20>.Loop(a);
        if (r == null) r = Custom<T21>.Loop(a);
        if (r == null) r = Generated<T21>.Loop(a);
        if (r == null) r = Custom<T22>.Loop(a);
        if (r == null) r = Generated<T22>.Loop(a);
        if (r == null) r = Custom<T23>.Loop(a);
        if (r == null) r = Generated<T23>.Loop(a);
        if (r == null) r = Custom<T24>.Loop(a);
        if (r == null) r = Generated<T24>.Loop(a);
        if (r == null) r = Custom<T25>.Loop(a);
        if (r == null) r = Generated<T25>.Loop(a);
        if (r == null) r = Custom<T26>.Loop(a);
        if (r == null) r = Generated<T26>.Loop(a);
        if (r == null) r = Custom<T27>.Loop(a);
        if (r == null) r = Generated<T27>.Loop(a);
        if (r == null) r = Custom<T28>.Loop(a);
        if (r == null) r = Generated<T28>.Loop(a);
        if (r == null) r = Custom<T29>.Loop(a);
        if (r == null) r = Generated<T29>.Loop(a);
        return r;
    }

    public static object? With(TokenArgs a)
    {
        if (a.Context == null) return null;
        var r = Custom<T00>.With(a);
        if (r == null) r = Generated<T00>.With(a);
        if (r == null) r = Custom<T01>.With(a);
        if (r == null) r = Generated<T01>.With(a);
        if (r == null) r = Custom<T02>.With(a);
        if (r == null) r = Generated<T02>.With(a);
        if (r == null) r = Custom<T03>.With(a);
        if (r == null) r = Generated<T03>.With(a);
        if (r == null) r = Custom<T04>.With(a);
        if (r == null) r = Generated<T04>.With(a);
        if (r == null) r = Custom<T05>.With(a);
        if (r == null) r = Generated<T05>.With(a);
        if (r == null) r = Custom<T06>.With(a);
        if (r == null) r = Generated<T06>.With(a);
        if (r == null) r = Custom<T07>.With(a);
        if (r == null) r = Generated<T07>.With(a);
        if (r == null) r = Custom<T08>.With(a);
        if (r == null) r = Generated<T08>.With(a);
        if (r == null) r = Custom<T09>.With(a);
        if (r == null) r = Generated<T09>.With(a);
        if (r == null) r = Custom<T10>.With(a);
        if (r == null) r = Generated<T10>.With(a);
        if (r == null) r = Custom<T11>.With(a);
        if (r == null) r = Generated<T11>.With(a);
        if (r == null) r = Custom<T12>.With(a);
        if (r == null) r = Generated<T12>.With(a);
        if (r == null) r = Custom<T13>.With(a);
        if (r == null) r = Generated<T13>.With(a);
        if (r == null) r = Custom<T14>.With(a);
        if (r == null) r = Generated<T14>.With(a);
        if (r == null) r = Custom<T15>.With(a);
        if (r == null) r = Generated<T15>.With(a);
        if (r == null) r = Custom<T16>.With(a);
        if (r == null) r = Generated<T16>.With(a);
        if (r == null) r = Custom<T17>.With(a);
        if (r == null) r = Generated<T17>.With(a);
        if (r == null) r = Custom<T18>.With(a);
        if (r == null) r = Generated<T18>.With(a);
        if (r == null) r = Custom<T19>.With(a);
        if (r == null) r = Generated<T19>.With(a);
        if (r == null) r = Custom<T20>.With(a);
        if (r == null) r = Generated<T20>.With(a);
        if (r == null) r = Custom<T21>.With(a);
        if (r == null) r = Generated<T21>.With(a);
        if (r == null) r = Custom<T22>.With(a);
        if (r == null) r = Generated<T22>.With(a);
        if (r == null) r = Custom<T23>.With(a);
        if (r == null) r = Generated<T23>.With(a);
        if (r == null) r = Custom<T24>.With(a);
        if (r == null) r = Generated<T24>.With(a);
        if (r == null) r = Custom<T25>.With(a);
        if (r == null) r = Generated<T25>.With(a);
        if (r == null) r = Custom<T26>.With(a);
        if (r == null) r = Generated<T26>.With(a);
        if (r == null) r = Custom<T27>.With(a);
        if (r == null) r = Generated<T27>.With(a);
        if (r == null) r = Custom<T28>.With(a);
        if (r == null) r = Generated<T28>.With(a);
        if (r == null) r = Custom<T29>.With(a);
        if (r == null) r = Generated<T29>.With(a);
        return r;
    }

}

public struct T00 { }
public struct T01 { }
public struct T02 { }
public struct T03 { }
public struct T04 { }
public struct T05 { }
public struct T06 { }
public struct T07 { }
public struct T08 { }
public struct T09 { }
public struct T10 { }
public struct T11 { }
public struct T12 { }
public struct T13 { }
public struct T14 { }
public struct T15 { }
public struct T16 { }
public struct T17 { }
public struct T18 { }
public struct T19 { }
public struct T20 { }
public struct T21 { }
public struct T22 { }
public struct T23 { }
public struct T24 { }
public struct T25 { }
public struct T26 { }
public struct T27 { }
public struct T28 { }
public struct T29 { }
