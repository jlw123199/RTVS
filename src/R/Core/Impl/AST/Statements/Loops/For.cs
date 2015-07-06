﻿using Microsoft.R.Core.AST.Definitions;
using Microsoft.R.Core.AST.Expressions;
using Microsoft.R.Core.Parser;

namespace Microsoft.R.Core.AST.Statements.Loops
{
    /// <summary>
    /// For statement
    /// </summary>
    public class For : KeywordExpressionScopeStatement
    {
        private static readonly string[] keywords = new string[] { "break", "next" };

        public EnumerableExpression EnumerableExpression { get; private set; }

        protected override bool ParseExpression(ParseContext context, IAstNode parent)
        {
            this.EnumerableExpression = new EnumerableExpression();
            if(this.EnumerableExpression.Parse(context, this))
            {
                return base.Parse(context, parent);
            }

            return false;
        }
    }
}