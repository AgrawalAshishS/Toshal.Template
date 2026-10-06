// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Providers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;

    /// <summary>
    /// Picks the providers for a tag by the type of the current context, so each provider sees only its own type.
    /// It replaces a long chain of "try this provider, then that one" with one lookup by type.
    /// Its <see cref="Token"/>, <see cref="Condition"/>, <see cref="Loop"/> and <see cref="With"/> methods fit the provider properties of
    /// <see cref="Processor"/>; <see cref="AttachTo(Processor)"/> sets all four.
    /// </summary>
    /// <remarks>
    /// <para>The order for one tag:</para>
    /// <list type="number">
    /// <item>the global providers registered with <see cref="GlobalOrder.BeforeTyped"/>, in the order you registered them;</item>
    /// <item>when the context is not null, the providers for its exact type, then for its base classes (nearest first, up to <see cref="object"/>),
    /// then for the interfaces it implements. Providers for the same type keep the order you registered them, so register a hand written
    /// provider before a generated one to let it win;</item>
    /// <item>the global providers registered with <see cref="GlobalOrder.AfterTyped"/>.</item>
    /// </list>
    /// <para>The first provider that handles the name wins, even with a null, empty or <c>false</c> value. When no provider handles it,
    /// a token, loop and with get null and a condition gets <c>false</c>.</para>
    /// <para>Speed: the list of providers for a type is built once, on the first tag with a context of that type, and then cached.
    /// Each later tag costs one dictionary lookup on the type and one call per provider tried. No reflection runs while processing,
    /// and nothing is allocated.</para>
    /// <para><b>Warning:</b> register every provider before the first lookup. After it, <see cref="Register{T}(ContextProvider{T})"/> and
    /// <see cref="RegisterGlobal(GlobalProvider, GlobalOrder)"/> throw. Lookups are safe from many threads at once.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var registry = new ContextProviderRegistry()
    ///     .Register(new CustomerProviderCustom())   // hand written: asked first
    ///     .Register(new CustomerProvider())         // generated
    ///     .Register(new OrderProvider())
    ///     .RegisterGlobal(new ProjectGlobals(), GlobalOrder.BeforeTyped);
    ///
    /// var processor = new Processor();
    /// registry.AttachTo(processor);
    /// string text = processor.Process(new ProcessorArgs(tokens) { Context = customer }).ToString();
    /// </code>
    /// </example>
    public sealed class ContextProviderRegistry
    {
        private readonly object gate = new object();
        private readonly List<Registration> registrations = new List<Registration>();
        private GlobalProvider[] before = Array.Empty<GlobalProvider>();
        private GlobalProvider[] after = Array.Empty<GlobalProvider>();

        // Never changed after it is published; a new type makes a copy. So reads need no lock.
        private Dictionary<Type, ProviderEntry[]> cache = new Dictionary<Type, ProviderEntry[]>();
        private volatile bool locked;

        /// <summary>
        /// Adds a provider for contexts of type <typeparamref name="T"/> and of every type that derives from it or implements it.
        /// </summary>
        /// <typeparam name="T">The type of context the provider answers for.</typeparam>
        /// <param name="provider">The provider.</param>
        /// <returns>This registry, so calls can be chained.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="provider"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The registry was already used for a lookup.</exception>
        /// <example>
        /// <code>
        /// registry.Register(new TableColumnProviderCustom()).Register(new TableColumnProvider());
        /// </code>
        /// </example>
        public ContextProviderRegistry Register<T>(ContextProvider<T> provider)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(provider);

            lock (this.gate)
            {
                this.ThrowIfLocked();
                this.registrations.Add(new Registration(typeof(T), new ProviderEntry<T>(provider)));
            }

            return this;
        }

        /// <summary>
        /// Adds a provider that is asked for every context, also a null one.
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <param name="order">Whether it is asked before or after the typed providers. The default is <see cref="GlobalOrder.AfterTyped"/>.</param>
        /// <returns>This registry, so calls can be chained.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="provider"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The registry was already used for a lookup.</exception>
        /// <example>
        /// <code>
        /// registry.RegisterGlobal(new ProjectGlobals(), GlobalOrder.BeforeTyped);
        /// </code>
        /// </example>
        public ContextProviderRegistry RegisterGlobal(GlobalProvider provider, GlobalOrder order = GlobalOrder.AfterTyped)
        {
            ArgumentNullException.ThrowIfNull(provider);

            lock (this.gate)
            {
                this.ThrowIfLocked();
                if (order == GlobalOrder.BeforeTyped)
                {
                    this.before = [.. this.before, provider];
                }
                else
                {
                    this.after = [.. this.after, provider];
                }
            }

            return this;
        }

        /// <summary>
        /// Sets <see cref="Processor.TokenValueProvider"/>, <see cref="Processor.ConditionValueProvider"/>, <see cref="Processor.LoopValueProvider"/>
        /// and <see cref="Processor.WithValueProvider"/> to this registry. <see cref="Processor.ProcessTemplateValueProvider"/> is not changed.
        /// </summary>
        /// <param name="processor">The processor to set.</param>
        /// <exception cref="ArgumentNullException"><paramref name="processor"/> is null.</exception>
        /// <example>
        /// <code>
        /// var processor = new Processor { ProcessTemplateValueProvider = args =&gt; templates[args.Name] };
        /// registry.AttachTo(processor);
        /// </code>
        /// </example>
        public void AttachTo(Processor processor)
        {
            ArgumentNullException.ThrowIfNull(processor);

            processor.TokenValueProvider = this.Token;
            processor.ConditionValueProvider = this.Condition;
            processor.LoopValueProvider = this.Loop;
            processor.WithValueProvider = this.With;
        }

        /// <summary>
        /// Answers <c>&lt;%=name%&gt;</c> with the first provider that handles the name. It fits <see cref="Processor.TokenValueProvider"/>.
        /// </summary>
        /// <param name="args">The arguments from the processor.</param>
        /// <returns>The text, or null when no provider handled the name.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> is null.</exception>
        /// <example>
        /// <code>
        /// processor.TokenValueProvider = registry.Token;
        /// </code>
        /// </example>
        public string? Token(TokenArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);
            if (!this.locked) this.Lock();

            string? value;
            var globals = this.before;
            for (var i = 0; i < globals.Length; i++)
            {
                if (globals[i].TryToken(args, out value)) return value;
            }

            var context = args.Context;
            if (context != null)
            {
                var entries = this.Resolve(context.GetType());
                for (var i = 0; i < entries.Length; i++)
                {
                    if (entries[i].TryToken(context, args, out value)) return value;
                }
            }

            globals = this.after;
            for (var i = 0; i < globals.Length; i++)
            {
                if (globals[i].TryToken(args, out value)) return value;
            }

            return null;
        }

        /// <summary>
        /// Answers <c>&lt;%IF name%&gt;</c> with the first provider that handles the name. It fits <see cref="Processor.ConditionValueProvider"/>.
        /// </summary>
        /// <param name="args">The arguments from the processor.</param>
        /// <returns>The value of the condition, or <c>false</c> when no provider handled the name.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> is null.</exception>
        /// <example>
        /// <code>
        /// processor.ConditionValueProvider = registry.Condition;
        /// </code>
        /// </example>
        public bool Condition(ConditionArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);
            if (!this.locked) this.Lock();

            bool value;
            var globals = this.before;
            for (var i = 0; i < globals.Length; i++)
            {
                if (globals[i].TryCondition(args, out value)) return value;
            }

            var context = args.Context;
            if (context != null)
            {
                var entries = this.Resolve(context.GetType());
                for (var i = 0; i < entries.Length; i++)
                {
                    if (entries[i].TryCondition(context, args, out value)) return value;
                }
            }

            globals = this.after;
            for (var i = 0; i < globals.Length; i++)
            {
                if (globals[i].TryCondition(args, out value)) return value;
            }

            return false;
        }

        /// <summary>
        /// Answers <c>&lt;%FOREACH name%&gt;</c> with the first provider that handles the name. It fits <see cref="Processor.LoopValueProvider"/>.
        /// </summary>
        /// <param name="args">The arguments from the processor.</param>
        /// <returns>The rows, or null when no provider handled the name.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> is null.</exception>
        /// <example>
        /// <code>
        /// processor.LoopValueProvider = registry.Loop;
        /// </code>
        /// </example>
        public IList? Loop(LoopArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);
            if (!this.locked) this.Lock();

            IList? value;
            var globals = this.before;
            for (var i = 0; i < globals.Length; i++)
            {
                if (globals[i].TryLoop(args, out value)) return value;
            }

            var context = args.Context;
            if (context != null)
            {
                var entries = this.Resolve(context.GetType());
                for (var i = 0; i < entries.Length; i++)
                {
                    if (entries[i].TryLoop(context, args, out value)) return value;
                }
            }

            globals = this.after;
            for (var i = 0; i < globals.Length; i++)
            {
                if (globals[i].TryLoop(args, out value)) return value;
            }

            return null;
        }

        /// <summary>
        /// Answers <c>&lt;%WITH name%&gt;</c> with the first provider that handles the name. It fits <see cref="Processor.WithValueProvider"/>.
        /// </summary>
        /// <param name="args">The arguments from the processor.</param>
        /// <returns>The context for the block, or null when no provider handled the name.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> is null.</exception>
        /// <example>
        /// <code>
        /// processor.WithValueProvider = registry.With;
        /// </code>
        /// </example>
        public object? With(TokenArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);
            if (!this.locked) this.Lock();

            object? value;
            var globals = this.before;
            for (var i = 0; i < globals.Length; i++)
            {
                if (globals[i].TryWith(args, out value)) return value;
            }

            var context = args.Context;
            if (context != null)
            {
                var entries = this.Resolve(context.GetType());
                for (var i = 0; i < entries.Length; i++)
                {
                    if (entries[i].TryWith(context, args, out value)) return value;
                }
            }

            globals = this.after;
            for (var i = 0; i < globals.Length; i++)
            {
                if (globals[i].TryWith(args, out value)) return value;
            }

            return null;
        }

        private void ThrowIfLocked()
        {
            if (this.locked)
            {
                throw new InvalidOperationException("Register every provider before the first lookup. This registry was already used.");
            }
        }

        private void Lock()
        {
            lock (this.gate)
            {
                this.locked = true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ProviderEntry[] Resolve(Type type)
        {
            return Volatile.Read(ref this.cache).TryGetValue(type, out var entries) ? entries : this.Build(type);
        }

        // Runs once per context type. Exact type, then base classes nearest first, then interfaces; registration order inside each.
        private ProviderEntry[] Build(Type type)
        {
            lock (this.gate)
            {
                if (this.cache.TryGetValue(type, out var existing)) return existing;

                var list = new List<ProviderEntry>();
                for (var current = type; current != null; current = current.BaseType)
                {
                    foreach (var registration in this.registrations)
                    {
                        if (registration.Type == current) list.Add(registration.Entry);
                    }
                }

                foreach (var registration in this.registrations)
                {
                    if (registration.Type.IsInterface && registration.Type.IsAssignableFrom(type)) list.Add(registration.Entry);
                }

                var entries = list.ToArray();
                var copy = new Dictionary<Type, ProviderEntry[]>(this.cache) { [type] = entries };
                Volatile.Write(ref this.cache, copy);
                return entries;
            }
        }

        private readonly record struct Registration(Type Type, ProviderEntry Entry);

        // Lets the registry call a ContextProvider<T> without knowing T.
        private abstract class ProviderEntry
        {
            public abstract bool TryToken(object context, TokenArgs args, out string? value);

            public abstract bool TryCondition(object context, ConditionArgs args, out bool value);

            public abstract bool TryLoop(object context, LoopArgs args, out IList? value);

            public abstract bool TryWith(object context, TokenArgs args, out object? value);
        }

        // The registry only hands a context to an entry whose type it is assignable to, so the unchecked cast is safe.
        private sealed class ProviderEntry<T> : ProviderEntry
            where T : class
        {
            private readonly ContextProvider<T> provider;

            public ProviderEntry(ContextProvider<T> provider)
            {
                this.provider = provider;
            }

            public override bool TryToken(object context, TokenArgs args, out string? value) => this.provider.TryToken(Unsafe.As<T>(context), args, out value);

            public override bool TryCondition(object context, ConditionArgs args, out bool value) => this.provider.TryCondition(Unsafe.As<T>(context), args, out value);

            public override bool TryLoop(object context, LoopArgs args, out IList? value) => this.provider.TryLoop(Unsafe.As<T>(context), args, out value);

            public override bool TryWith(object context, TokenArgs args, out object? value) => this.provider.TryWith(Unsafe.As<T>(context), args, out value);
        }
    }
}
