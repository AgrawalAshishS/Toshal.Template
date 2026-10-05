// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Processor.cs" company="Toshal Infotech">
//   http://www.ToshalInfotech.com
//   Copyright (c) 2014-2015
//   by Toshal Infotech
//   
//   Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
//   documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
//   the rights to use, copy, modify, merge, publish, distribute, sub-license, and/or sell copies of the Software, and 
//   to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//   
//   The above copyright notice and this permission notice shall be included in all copies or substantial portions 
//   of the Software.
//   
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
//   TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
//   THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
//   CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
//   DEALINGS IN THE SOFTWARE.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Toshal.Template
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;

    using Toshal.Template.Tokens;

    /// <summary>
    ///     The processor.
    /// </summary>
    public class Processor
    {
        #region Public Methods and Operators

        /// <summary>
        ///     The process.
        /// </summary>
        /// <param name="args">
        ///     The args.
        /// </param>
        /// <returns>
        ///     The <see cref="StringBuilder" />.
        /// </returns>
        public StringBuilder Process(ProcessorArgs args)
        {
            var retVal = new StringBuilder();
            var parentContext = new List<object?>();
            if (args.Context != null) { parentContext.Add(args.Context); }

            this.Process(retVal, args.TokenList, args.Context, parentContext, new Dictionary<string, string>());

            return retVal;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets the condition value provider.
        /// </summary>
        public Func<ConditionArgs, bool>? ConditionValueProvider { get; set; }

        /// <summary>
        ///     Gets or sets the loop value provider.
        /// </summary>
        public Func<LoopArgs, IList?>? LoopValueProvider { get; set; }

        /// <summary>
        ///     Gets or sets the token value provider.
        /// </summary>
        public Func<TokenArgs, string?>? TokenValueProvider { get; set; }

        /// <summary>
        ///     Gets or sets the with value provider.
        /// </summary>
        public Func<TokenArgs, object?>? WithValueProvider { get; set; }

        /// <summary>
        ///     Gets or sets the process template value provider.
        /// </summary>
        public Func<ProcessTemplateArgs, List<IToken>?>? ProcessTemplateValueProvider { get; set; }

        #endregion

        /// <summary>
        ///     The handle condition token.
        /// </summary>
        /// <param name="output">
        ///     The output.
        /// </param>
        /// <param name="token">
        ///     The token.
        /// </param>
        /// <param name="context">
        ///     The context.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        private bool HandleConditionToken(StringBuilder output, IToken token, object? context, List<object?> parentContext, Dictionary<string, string> customTokens)
        {
            var conditionToken = token as ConditionToken;
            if (conditionToken == null) return false;
            if (this.ConditionValueProvider == null) return true; //skip as there is no value provider

            var args = new ConditionArgs(conditionToken, context, parentContext);
            var val = this.ConditionValueProvider(args);

            if (conditionToken.IsPositive == val)
            {
                this.Process(output, conditionToken.InnerTokens, context, parentContext, customTokens);
            }
            else if (conditionToken.FalsePart != null)
            {
                var elseToken = conditionToken.FalsePart as ElseToken;
                if (elseToken != null)
                {
                    this.Process(output, elseToken.InnerTokens, context, parentContext, customTokens);
                }
                else
                {
                    this.HandleConditionToken(output, conditionToken.FalsePart, context, parentContext, customTokens);
                }
            }

            return true;
        }

        /// <summary>
        ///     The handle for each token.
        /// </summary>
        /// <param name="output">
        ///     The output.
        /// </param>
        /// <param name="token">
        ///     The token.
        /// </param>
        /// <param name="context">
        ///     The context.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        private bool HandleForEachToken(StringBuilder output, IToken token, object? context, List<object?> parentContext, Dictionary<string, string> customTokens)
        {
            var forEachToken = token as ForEachToken;
            if (forEachToken == null) return false;
            if (this.LoopValueProvider == null) return true; //skip as there is no value provider

            var args = new LoopArgs(forEachToken, context, parentContext);
            return this.ProcessForEach(output, context, args, forEachToken, customTokens);
        }

        /// <summary>
        ///     The handle named token.
        /// </summary>
        /// <param name="output">
        ///     The output.
        /// </param>
        /// <param name="token">
        ///     The token.
        /// </param>
        /// <param name="context">
        ///     The context.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        private bool HandleNamedToken(StringBuilder output, IToken token, object? context, List<object?> parentContext, Dictionary<string, string> customTokens)
        {
            var namedToken = token as NamedToken;
            if (namedToken == null) return false;

            string? value = "";
            if (customTokens.TryGetValue(namedToken.Name, out value))
            {
                output.Append(value);
                return true;
            }

            if (this.TokenValueProvider == null) return true; //skip as there is no value provider

            var args = new TokenArgs(namedToken, context, parentContext);
            var val = this.TokenValueProvider(args);

            if (!string.IsNullOrEmpty(val))
            {
                output.Append(val);
            }

            return true;
        }

        /// <summary>
        ///     The handle reuse for each token.
        /// </summary>
        /// <param name="output">
        ///     The output.
        /// </param>
        /// <param name="token">
        ///     The token.
        /// </param>
        /// <param name="context">
        ///     The context.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        private bool HandleReuseForEachToken(StringBuilder output, IToken token, object? context, List<object?> parentContext, Dictionary<string, string> customTokens)
        {
            var reuseForEachToken = token as ReuseForEachToken;
            if (reuseForEachToken == null) return false;
            if (this.LoopValueProvider == null) return true;

            var args = new LoopArgs(reuseForEachToken.Name, context, parentContext, reuseForEachToken.ExistingForEachToken!.Attributes);
            return this.ProcessForEach(output, context, args, reuseForEachToken.ExistingForEachToken!, customTokens);
        }

        /// <summary>
        ///     The handle with token.
        /// </summary>
        /// <param name="output">
        ///     The output.
        /// </param>
        /// <param name="token">
        ///     The token.
        /// </param>
        /// <param name="context">
        ///     The context.
        /// </param>
        private bool HandleWithToken(StringBuilder output, IToken token, object? context, List<object?> parentContext, Dictionary<string, string> customTokens)
        {
            var withToken = token as WithToken;
            if (withToken == null) return false;
            if (this.WithValueProvider == null) return true;//skip as there is no value provider;

            var args = new TokenArgs(withToken, context, parentContext);

            var val = this.WithValueProvider(args);
            if (val != null)
            {
                parentContext.Add(val);
                var newCustomTokens = new Dictionary<string, string>(customTokens);
                this.Process(output, withToken.InnerTokens, val, parentContext, newCustomTokens);
                parentContext.Remove(val);
            }

            return true;
        }

        /// <summary>
        ///     The handle SET token.
        /// </summary>
        /// <param name="output">
        ///     The output.
        /// </param>
        /// <param name="token">
        ///     The token.
        /// </param>
        /// <param name="context">
        ///     The context.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        private bool HandleSetToken(IToken token, object? context, List<object?> parentContext, ref Dictionary<string, string> customTokens)
        {
            var setToken = token as SetToken;
            if (setToken == null) return false;

            var valueOutput = new StringBuilder();
            this.Process(valueOutput, setToken.InnerTokens, context, parentContext, customTokens);

            if (customTokens.ContainsKey(setToken.Name) == false)
            {
                customTokens.Add(setToken.Name, valueOutput.ToString());
            }
            else
            {
                customTokens[setToken.Name] = valueOutput.ToString();
            }

            return true;
        }

        private bool HandleRemovePreviousNewLine(StringBuilder output, IToken token, object? context, Dictionary<string, string> customTokens)
        {
            var removeToken = token as RemovePreviousNewLineToken;
            if (removeToken == null) return false;

            if (output.Length == 0) return true;

            if (output[output.Length - 1] == '\n')
                output.Remove(output.Length - 1, 1);

            if (output.Length > 0 && output[output.Length - 1] == '\r')
                output.Remove(output.Length - 1, 1);

            return true;
        }

        private bool HandleRemovePreviousChars(StringBuilder output, IToken token, object? context, Dictionary<string, string> customTokens)
        {
            var removeToken = token as RemovePreviousCharsToken;
            if (removeToken == null) return false;

            var count = removeToken.CharCount;
            if (output.Length < removeToken.CharCount) count = output.Length;

            output.Remove(output.Length - count, count);

            return true;
        }

        private bool HandleProcessTemplateToken(StringBuilder output, IToken token, object? context, List<object?> parentContext, Dictionary<string, string> customTokens)
        {
            var processTemplateToken = token as ProcessTemplateToken;
            if (processTemplateToken == null) return false;
            if (this.ProcessTemplateValueProvider == null) return true;//skip as there is no value provider;

            var args = new ProcessTemplateArgs(processTemplateToken, context, parentContext);

            var val = this.ProcessTemplateValueProvider(args);
            if (val != null)
            {
                var newCustomTokens = new Dictionary<string, string>(customTokens);
                this.Process(output, val, context, parentContext, newCustomTokens);
            }

            return true;
        }

        private bool HandleContextAsString(StringBuilder output, IToken token, object? context)
        {
            var contextToken = token as ContextAsStringToken;
            if (contextToken == null) return false;

            if (context == null) return true;

            output.Append(context.ToString());

            return true;
        }

        /// <summary>
        ///     The process.
        /// </summary>
        /// <param name="output">
        ///     The output.
        /// </param>
        /// <param name="tokenList">
        ///     The token list.
        /// </param>
        /// <param name="context">
        ///     The context.
        /// </param>
        private void Process(StringBuilder output, List<IToken> tokenList, object? context, List<object?> parentContext, Dictionary<string, string> customTokens)
        {
            foreach (var token in tokenList)
            {
                var contentToken = token as ContentToken;
                if (contentToken != null)
                {
                    output.Append(contentToken.Content);
                    continue;
                }

                if (this.HandleNamedToken(output, token, context, parentContext, customTokens))
                {
                    continue;
                }

                if (this.HandleConditionToken(output, token, context, parentContext, customTokens))
                {
                    continue;
                }

                if (this.HandleForEachToken(output, token, context, parentContext, customTokens))
                {
                    continue;
                }

                if (this.HandleReuseForEachToken(output, token, context, parentContext, customTokens))
                {
                    continue;
                }

                if (this.HandleWithToken(output, token, context, parentContext, customTokens))
                {
                    continue;
                }

                if (this.HandleRemovePreviousNewLine(output, token, context, customTokens))
                {
                    continue;
                }

                if (this.HandleRemovePreviousChars(output, token, context, customTokens))
                {
                    continue;
                }

                if (this.HandleProcessTemplateToken(output, token, context, parentContext, customTokens))
                {
                    continue;
                }

                if (this.HandleContextAsString(output, token, context))
                {
                    continue;
                }

                HandleSetToken(token, context, parentContext, ref customTokens);
            }
        }

        /// <summary>
        ///     The process for each.
        /// </summary>
        /// <param name="output">
        ///     The output.
        /// </param>
        /// <param name="context">
        ///     The context.
        /// </param>
        /// <param name="args">
        ///     The args.
        /// </param>
        /// <param name="forEachToken">
        ///     The for each token.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        private bool ProcessForEach(StringBuilder output, object? context, LoopArgs args, ForEachToken forEachToken, Dictionary<string, string> customTokens)
        {
            var rowLevelShared = new Dictionary<string, string>(customTokens);

            var val = this.LoopValueProvider!(args);
            if (val == null || val.Count == 0)
            {
                if (forEachToken.NoRecordTokens.Count > 0)
                {
                    var newCustomTokens = new Dictionary<string, string>(customTokens);
                    this.Process(output, forEachToken.NoRecordTokens, context, args.ParentContext, newCustomTokens);
                }

                return true;
            }

            args.ParentContext.Add(val);

            if (forEachToken.HeaderTokens.Count > 0)
            {
                var newCustomTokens = new Dictionary<string, string>(customTokens);
                this.Process(output, forEachToken.HeaderTokens, val, args.ParentContext, newCustomTokens);
            }

            for (var i = 0; i < val.Count; i++)
            {
                var item = val[i];
                var isAlt = i % 2 == 1;

                args.ParentContext.Add(item);

                var beforeTokens = (isAlt && forEachToken.BeforeAltRowTokens.Count > 0)
                                       ? forEachToken.BeforeAltRowTokens
                                       : forEachToken.BeforeRowTokens;

                var afterTokens = (isAlt && forEachToken.AfterAltRowTokens.Count > 0)
                                      ? forEachToken.AfterAltRowTokens
                                      : forEachToken.AfterRowTokens;

                var rowTokens = (isAlt && forEachToken.AltRowTokens.Count > 0)
                                    ? forEachToken.AltRowTokens
                                    : forEachToken.RowTokens;

                if (i == 0 && val.Count > 1)
                {
                    beforeTokens = (forEachToken.BeforeFirstRowTokens.Count > 0)
                                       ? forEachToken.BeforeFirstRowTokens
                                       : beforeTokens;

                    afterTokens = (forEachToken.AfterFirstRowTokens.Count > 0)
                                      ? forEachToken.AfterFirstRowTokens
                                      : afterTokens;

                    rowTokens = (forEachToken.FirstRowTokens.Count > 0) ? forEachToken.FirstRowTokens : rowTokens;
                }
                else if (i == val.Count - 1)
                {
                    beforeTokens = (forEachToken.BeforeLastRowTokens.Count > 0)
                                       ? forEachToken.BeforeLastRowTokens
                                       : beforeTokens;

                    afterTokens = (forEachToken.AfterLastRowTokens.Count > 0)
                                      ? forEachToken.AfterLastRowTokens
                                      : afterTokens;

                    rowTokens = (forEachToken.LastRowTokens.Count > 0) ? forEachToken.LastRowTokens : rowTokens;
                }

                if (beforeTokens.Count > 0)
                {
                    var newCustomTokens = new Dictionary<string, string>(rowLevelShared);
                    this.Process(output, beforeTokens, item, args.ParentContext, newCustomTokens);
                }

                if (rowTokens.Count > 0)
                {
                    this.Process(output, rowTokens, item, args.ParentContext, rowLevelShared);
                }

                if (afterTokens.Count > 0)
                {
                    var newCustomTokens = new Dictionary<string, string>(rowLevelShared);
                    this.Process(output, afterTokens, item, args.ParentContext, newCustomTokens);
                }

                args.ParentContext.Remove(item);
            }

            if (forEachToken.FooterTokens.Count > 0)
            {
                var newCustomTokens = new Dictionary<string, string>(customTokens);
                this.Process(output, forEachToken.FooterTokens, val, args.ParentContext, newCustomTokens);
            }

            args.ParentContext.Remove(val);

            return true;
        }
    }
}