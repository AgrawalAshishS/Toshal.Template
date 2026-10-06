// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

// The parser sources are written for .NET 10. These few members let them compile for netstandard2.0, where the source generator runs.
// They only exist in this assembly.

namespace System
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Runtime.CompilerServices;

    internal static class NetStandardPolyfills
    {
        extension(ArgumentNullException)
        {
            public static void ThrowIfNull(object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
            {
                if (argument is null) throw new ArgumentNullException(paramName);
            }
        }

        extension(ArgumentOutOfRangeException)
        {
            public static void ThrowIfNegative(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
            {
                if (value < 0) throw new ArgumentOutOfRangeException(paramName, value, "The value must not be negative.");
            }
        }

        extension<TKey, TValue>(ReadOnlyDictionary<TKey, TValue>)
            where TKey : notnull
        {
            public static ReadOnlyDictionary<TKey, TValue> Empty => EmptyDictionary<TKey, TValue>.Value;
        }

        extension(ReadOnlySpan<char> span)
        {
            public int Count(char value)
            {
                int count = 0;
                foreach (var c in span)
                {
                    if (c == value) count++;
                }

                return count;
            }
        }

        extension<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
            where TKey : notnull
        {
            public bool TryAdd(TKey key, TValue value)
            {
                if (dictionary.ContainsKey(key)) return false;
                dictionary.Add(key, value);
                return true;
            }
        }

        private static class EmptyDictionary<TKey, TValue>
            where TKey : notnull
        {
            public static readonly ReadOnlyDictionary<TKey, TValue> Value = new ReadOnlyDictionary<TKey, TValue>(new Dictionary<TKey, TValue>());
        }
    }

    // Lets the compiler turn text[^1] into text[text.Length - 1].
    internal readonly struct Index
    {
        private readonly int value;

        public Index(int value, bool fromEnd = false)
        {
            this.value = fromEnd ? ~value : value;
        }

        public static implicit operator Index(int value) => new Index(value);

        public bool IsFromEnd => this.value < 0;

        public int Value => this.value < 0 ? ~this.value : this.value;

        public int GetOffset(int length) => this.IsFromEnd ? length - this.Value : this.Value;
    }
}

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    internal sealed class CallerArgumentExpressionAttribute : Attribute
    {
        public CallerArgumentExpressionAttribute(string parameterName)
        {
            this.ParameterName = parameterName;
        }

        public string ParameterName { get; }
    }

    internal static class IsExternalInit
    {
    }
}

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    internal sealed class NotNullWhenAttribute : Attribute
    {
        public NotNullWhenAttribute(bool returnValue)
        {
            this.ReturnValue = returnValue;
        }

        public bool ReturnValue { get; }
    }
}

namespace System.Collections.Generic
{
    using System.Runtime.CompilerServices;

    internal sealed class ReferenceEqualityComparer : IEqualityComparer<object?>
    {
        public static ReferenceEqualityComparer Instance { get; } = new ReferenceEqualityComparer();

        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

        public int GetHashCode(object? obj) => RuntimeHelpers.GetHashCode(obj);
    }

    internal static class DictionaryPolyfills
    {
        public static TValue? GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
            where TKey : notnull
        {
            return dictionary.TryGetValue(key, out var value) ? value : default;
        }
    }
}
