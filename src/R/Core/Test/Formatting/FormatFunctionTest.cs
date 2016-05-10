﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.Languages.Core.Formatting;
using Microsoft.R.Core.Formatting;
using Microsoft.UnitTests.Core.XUnit;
using Xunit;

namespace Microsoft.R.Core.Test.Formatting {
    [ExcludeFromCodeCoverage]
    [Category.R.Formatting]
    public class FormatFunctionTest {
        [CompositeTest]
        [InlineData("function(a,b) {\nreturn(a+b)}", "function(a, b) {\n  return(a + b)\n}")]
        [InlineData("function(a,b) {return(a+b)}", "function(a, b) { return(a + b) }")]
        [InlineData("function(a,b) {{return(a+b)}}", "function(a, b) {{ return(a + b) }}")]
        [InlineData("function(a,b) a+b", "function(a, b) a + b")]
        public void FormatFunction(string original, string expected) {
            RFormatter f = new RFormatter();
            string actual = f.Format(original);
            actual.Should().Be(expected);
        }

        [Test]
        public void FormatFunctionAlignArguments() {
            RFormatOptions options = new RFormatOptions();
            options.IndentType = IndentType.Tabs;
            options.TabSize = 2;

            RFormatter f = new RFormatter(options);
            string original = "x <- function (x,  \n intercept=TRUE, tolerance =1e-07, \n    yname = NULL)\n";
            string actual = f.Format(original);
            string expected = "x <- function(x,\n intercept = TRUE, tolerance = 1e-07,\n\t\tyname = NULL)\n";

            actual.Should().Be(expected);
        }

        [CompositeTest]
        [InlineData("x <- func(a,{return(b)})", "x <- func(a, {\n  return(b)\n})")]
        [InlineData("x<-func({return(b)})", "x <- func({\n  return(b)\n})")]
        public void FunctionInlineScope(string original, string expected) {
            RFormatter f = new RFormatter();
            string actual = f.Format(original);
            actual.Should().Be(expected);
        }

        [CompositeTest]
        [InlineData("x <- func(a,{if(TRUE) {x} else {y}})", "x <- func(a, {\n  if (TRUE) {\n    x\n  } else {\n    y\n  }\n})")]
        [InlineData("x <- func(a,{if(TRUE) 1 else 2})", "x <- func(a, {\n  if (TRUE) 1 else 2\n})")]
        [InlineData("x <- func(a,{\nif(TRUE) 1 else 2})", "x <- func(a, {\n  if (TRUE) 1 else 2\n})")]
        [InlineData("x <- func(a,{\nif(TRUE) {1} else {2}})", "x <- func(a, {\n  if (TRUE) {\n    1\n  } else {\n    2\n  }\n})")]
        [InlineData("x <- func(a,{\n        if(TRUE) {1} \n        else {2}\n })", "x <- func(a, {\n  if (TRUE) {\n    1\n  } else {\n    2\n  }\n})")]
        [InlineData("x <- func(a,\n   {\n      if(TRUE) 1 else 2\n   })", "x <- func(a, {\n  if (TRUE) 1 else 2\n})")]
        public void FunctionInlineIf(string original, string expected) {
            RFormatter f = new RFormatter();
            string actual = f.Format(original);
            actual.Should().Be(expected);
        }

        [Test]
        public void FormatFunctionInlineIf02() {
            RFormatter f = new RFormatter();

            string original = "x <- func(a,\n   {\n      if(TRUE) \n        if(FALSE) {x <-1} else x<-2\nelse\n        if(z) x <-1 else {5}\n    })";
            string actual = f.Format(original);
            string expected =
"x <- func(a, {\n" +
"  if (TRUE)\n" +
"    if (FALSE) {\n" +
"      x <- 1\n" +
"    } else\n" +
"      x <- 2\n" +
"  else\n" +
"    if (z) x <- 1 else {\n" +
"      5\n" +
"    }\n" +
"})";

            actual.Should().Be(expected);
        }

        [CompositeTest]
        [InlineData("function(a, b) {return(a+b)}", "function(a,b) { return(a + b) }")]
        [InlineData("function(a, b) {return(a+b)\n}", "function(a,b) {\n  return(a + b)\n}")]
        public void FormatFunctionNoSpaceAfterComma(string original, string expected) {
            RFormatOptions options = new RFormatOptions();
            options.SpaceAfterComma = false;

            RFormatter f = new RFormatter(options);
            string actual = f.Format(original);
            actual.Should().Be(expected);
        }
    }
}
