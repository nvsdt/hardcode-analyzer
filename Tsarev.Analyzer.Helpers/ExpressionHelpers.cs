﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Tsarev.Analyzer.Helpers
{
  /// <summary>
  /// Helpers that work with expressions
  /// </summary>
  public static class ExpressionHelpers
  {
    /// <summary>
    /// Is node constant literal or not
    /// </summary>
    public static bool IsConstant (this ExpressionSyntax syntax)
    {
      if (syntax is InitializerExpressionSyntax initializer)
      {
        return initializer.Expressions.All(expression => expression.IsConstant());
      }
      return syntax.IsKind(SyntaxKind.CharacterLiteralExpression, SyntaxKind.TrueLiteralExpression, SyntaxKind.FalseLiteralExpression, SyntaxKind.NumericLiteralExpression, SyntaxKind.StringLiteralExpression);
    }

    /// <summary>
    /// Is some node is one kind or another
    /// </summary>
    public static bool IsKind(this SyntaxNode node, params SyntaxKind[] kinds) => node != null && kinds.Contains(node.Kind());

    /// <summary>
    /// Get int constant value from constant
    /// </summary>
    public static int? GetIntOrDefault(this LiteralExpressionSyntax contextNode, SyntaxNodeAnalysisContext context)
    {
      var constant = context.SemanticModel.GetConstantValue(contextNode);
      return constant.HasValue ? (int?) (int) constant.Value : null;
    }
  }
}
