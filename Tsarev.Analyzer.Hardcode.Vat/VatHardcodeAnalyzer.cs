﻿using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Tsarev.Analyzer.Helpers;

namespace Tsarev.Analyzer.Hardcode.Vat
{
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class VatHardcodeAnalyzer : DiagnosticAnalyzer
  {

    private static readonly LocalizableString Title =
      new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager,
        typeof(Resources));

    private static readonly LocalizableString MessageFormat =
      new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat),
        Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString Description =
      new LocalizableResourceString(nameof(Resources.AnalyzerDescription),
        Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      nameof(VatHardcodeAnalyzer),
      Title,
      MessageFormat,
      "Hardcode",
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
      ImmutableArray.Create(Rule, StandartRules.FailedRule);

    private const int VatValue = 18;

    private static readonly decimal[] VatVariants = {
      VatValue, 100 - VatValue, 100 + VatValue, 1 - 0.01m * VatValue, 1 + 0.01m * VatValue
    };

    private static readonly string[] WhiteListParameters = {"index", "startindex", "length" };

    public override void Initialize(AnalysisContext context)
      => context.RegisterSafeSyntaxNodeAction(AnalyzeNumericLiterals,
        SyntaxKind.NumericLiteralExpression);

    private static void AnalyzeNumericLiterals(SyntaxNodeAnalysisContext context)
    {
      if (!(context.Node is LiteralExpressionSyntax literal)) return;

      var containingClass = context.Node.GetContainingClass();

      if (IsProbablyMigration(containingClass))
      {
        return;
      }

      if (literal.Parent is AttributeArgumentSyntax attributeArgumentSyntax)
      {
        var parameter =  attributeArgumentSyntax.GetCorrespondingParameter(context);
        if (IsWhiteListedParameter(parameter))
        {
          return;
        }
      }

      if (literal.Parent is ArgumentSyntax argumentSyntax)
      {
        if (literal.Parent.Parent is BracketedArgumentListSyntax)
        {
          //that's probably something like arr[18], which is stupid, but should be allowed
          return;
        }

        var parameter = argumentSyntax.GetCorrespondingParameter(context);
        if (IsWhiteListedParameter(parameter))
        {
          return;
        }
      }

      var value = literal.GetNumericOrDefault(context);
      if (value != null && VatVariants.Any(variant => variant == value))
      {
        context.ReportDiagnostic(
          Diagnostic.Create(Rule, context.Node.GetLocation(),
            value.Value.ToString(CultureInfo.InvariantCulture)));
      }
    }

    private static bool IsWhiteListedParameter(IParameterSymbol parameter) => parameter != null &&
                                                                              WhiteListParameters
                                                                                .Contains(parameter
                                                                                  .Name
                                                                                  .ToLowerInvariant());

    private static bool IsProbablyMigration([CanBeNull] ClassDeclarationSyntax containingClass) =>
      containingClass?.BaseList?.Types.Any(c => IsProbablyDbMigration(c.Type)) ?? false;

    private static bool IsProbablyDbMigration(TypeSyntax baseClass)
    {
      var baseClassName = (baseClass as IdentifierNameSyntax)?.Identifier.Text;
      return baseClassName == "DbMigration";
    }
  }
}
