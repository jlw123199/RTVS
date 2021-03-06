﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Languages.Core.Text;
using Microsoft.R.Support.Help;
using Microsoft.R.Support.Help.Functions;
using Microsoft.R.Support.RD.Tokens;

namespace Microsoft.R.Support.RD.Parser {
    /// <summary>
    /// Parser of the RD (R Documentation) file format. Primary usage 
    /// of the parser is the extraction of information on functions 
    /// and their parameters so we can show signature completion 
    /// and quick info in the VS editor.
    /// </summary>
    public static class RdParser {
        /// <summary>
        /// Given RD data and function name parses the data and creates structured
        /// information about the function. Method returns multiple functions since
        /// RD data often provides information on several functions so in order
        /// to avoid processing same data multiple times parser extracts information
        /// on all related functions.
        /// </summary>
        public static IReadOnlyList<IFunctionInfo> GetFunctionInfos(string rdHelpData) {
            var tokenizer = new RdTokenizer(tokenizeRContent: false);

            ITextProvider textProvider = new TextStream(rdHelpData);
            IReadOnlyTextRangeCollection<RdToken> tokens = tokenizer.Tokenize(textProvider, 0, textProvider.Length);
            RdParseContext context = new RdParseContext(tokens, textProvider);

            return ParseFunctions(context);
        }

        private static IReadOnlyList<IFunctionInfo> ParseFunctions(RdParseContext context) {
            IReadOnlyList<ISignatureInfo> signatureInfos = null;
            IReadOnlyDictionary<string, string> argumentDescriptions = null;
            var aliases = new List<string>();
            string functionDescription = null; // Description is normally one for all similar functions
            bool isInternal = false;
            string returnValue = null;
            string primaryName = null;

            while (!context.Tokens.IsEndOfStream() &&
                   (functionDescription == null || argumentDescriptions == null ||
                    signatureInfos == null || returnValue == null)) {
                RdToken token = context.Tokens.CurrentToken;

                if (context.IsAtKeywordWithParameters()) {
                    if (string.IsNullOrEmpty(functionDescription) && context.IsAtKeyword(@"\description")) {
                        functionDescription = RdText.GetText(context);
                    } else if (context.IsAtKeyword(@"\keyword")) {
                        string keyword = RdText.GetText(context);
                        if (!string.IsNullOrEmpty(keyword) && keyword.Contains("internal")) {
                            isInternal = true;
                        }
                    } else if (string.IsNullOrEmpty(returnValue) && context.IsAtKeyword(@"\value")) {
                        returnValue = RdText.GetText(context);
                    } else if (argumentDescriptions == null && context.IsAtKeyword(@"\arguments")) {
                        // Extract arguments and their descriptions
                        argumentDescriptions = RdArgumentDescription.ExtractArgumentDecriptions(context);
                    } else if (signatureInfos == null && context.IsAtKeyword(@"\usage")) {
                        // Extract signatures with function names
                        signatureInfos = RdFunctionSignature.ExtractSignatures(context);
                    } else if (context.IsAtKeyword(@"\alias")) {
                        var alias = RdText.GetText(context);
                        if (!string.IsNullOrWhiteSpace(alias)) {
                            aliases.Add(alias);
                        }
                    } else if (primaryName == null && context.IsAtKeyword(@"\name")) {
                        primaryName = RdText.GetText(context);
                    } else {
                        context.Tokens.Advance(2);
                    }
                } else {
                    context.Tokens.MoveToNextToken();
                }
            }

            // Merge descriptions into signatures. Add all arguments
            // listed in the \arguments{} section since function signature
            // does not always list all possible arguments.
            if (argumentDescriptions != null && signatureInfos != null) {
                foreach (ISignatureInfo sigInfo in signatureInfos) {
                    // Add missing arguments from the \arguments{} section
                    foreach (string name in argumentDescriptions.Keys) {
                        // TODO: do we need HashSet here instead? Generally arguments
                        // list is relatively short, about 10 items on average.
                        if (sigInfo.Arguments.FirstOrDefault(x => x.Name.Equals(name)) == null) {
                            sigInfo.Arguments.Add(new ArgumentInfo(name));
                        }
                    }

                    // Add description if it is not there yet
                    foreach (var arg in sigInfo.Arguments.Where(x => string.IsNullOrEmpty(x.Description))) {
                        string description;
                        if (argumentDescriptions.TryGetValue(arg.Name, out description)) {
                            ((NamedItemInfo)arg).Description = description ?? string.Empty;
                        }
                    }
                }
            }

            // Merge signatures into function infos
            var functionInfos = new Dictionary<string, FunctionInfo>();
            if (signatureInfos != null) {
                var functionSignatures = new Dictionary<string, List<ISignatureInfo>>();
                foreach (ISignatureInfo sigInfo in signatureInfos) {
                    FunctionInfo functionInfo;
                    List<ISignatureInfo> sigList;
                    if (!functionInfos.TryGetValue(sigInfo.FunctionName, out functionInfo)) {
                        // Create function info
                        functionInfo = CreateFunctionInfo(sigInfo.FunctionName, functionDescription, returnValue, isInternal);
                        functionInfos[sigInfo.FunctionName] = functionInfo;

                        // Create list of signatures for this function
                        sigList = new List<ISignatureInfo>();
                        functionSignatures[sigInfo.FunctionName] = sigList;
                        functionInfo.Signatures = sigList;
                    } else {
                        sigList = functionSignatures[sigInfo.FunctionName];
                    }

                    sigList.Add(sigInfo);
                }
            }

            // Propage to aliases
            if (!string.IsNullOrWhiteSpace(primaryName)) {
                FunctionInfo functionInfo;
                if (functionInfos.TryGetValue(primaryName, out functionInfo)) {
                    foreach (var alias in aliases) {
                        if (!functionInfos.ContainsKey(alias)) {
                            functionInfos[alias] = new FunctionInfo(alias, functionInfo);
                        }
                    }
                }
            }

            return functionInfos.Values.ToList();
        }

        private static FunctionInfo CreateFunctionInfo(string functionName, string functionDescription, string returnValue, bool isInternal) {
            var functionInfo = new FunctionInfo(functionName, functionDescription);
            functionInfo.IsInternal = isInternal;
            functionInfo.ReturnValue = returnValue;
            return functionInfo;
        }
    }
}