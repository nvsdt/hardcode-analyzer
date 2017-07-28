﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tsarev.Analyzer.TestHelpers
{
  /// <summary>
  /// Superclass of all Unit Tests for DiagnosticAnalyzers
  /// </summary>
  public abstract partial class DiagnosticVerifier
  {
    /// <summary>
    /// Get the CSharp analyzer being tested - to be implemented in non-abstract class
    /// </summary>
    protected abstract DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer();

    #region Verifier wrappers

    /// <summary>
    /// Called to test a C# DiagnosticAnalyzer when applied on the single inputted string as a source
    /// Note: input a DiagnosticResult for each Diagnostic expected
    /// </summary>
    /// <param name="sources">An array of strings to create source documents from to run the analyzers on</param>
    /// <param name="expected">DiagnosticResults that should appear after the analyzer is run on the sources</param>
    protected void VerifyCSharpDiagnostic(string sources, params DiagnosticResult[] expected)
    {
      var analyzer = GetCSharpDiagnosticAnalyzer();
      var diagnostics = GetSortedDiagnostics(new []{ sources}, LanguageNames.CSharp, analyzer);
      VerifyDiagnosticResults(diagnostics, analyzer, expected);
    }

    #endregion

    #region Actual comparisons and verifications
    /// <summary>
    /// Checks each of the actual Diagnostics found and compares them with the corresponding DiagnosticResult in the array of expected results.
    /// Diagnostics are considered equal only if the DiagnosticResultLocation, Id, Severity, and Message of the DiagnosticResult match the actual diagnostic.
    /// </summary>
    /// <param name="actualResults">The Diagnostics found by the compiler after running the analyzer on the source code</param>
    /// <param name="analyzer">The analyzer that was being run on the sources</param>
    /// <param name="expectedResults">Diagnostic Results that should have appeared in the code</param>
    private static void VerifyDiagnosticResults(IReadOnlyList<Diagnostic> actualResults, DiagnosticAnalyzer analyzer, params DiagnosticResult[] expectedResults)
    {
      var expectedCount = expectedResults.Length;
      var actualCount = actualResults.Count;

      if (expectedCount != actualCount)
      {
        var diagnosticsOutput = actualResults.Any() ? FormatDiagnostics(analyzer, actualResults.ToArray()) : "    NONE.";

        Assert.IsTrue(false,
          $"Mismatch between number of diagnostics returned, expected \"{expectedCount}\" actual \"{actualCount}\"\r\n\r\nDiagnostics:\r\n{diagnosticsOutput}\r\n");
      }

      for (var i = 0; i < expectedResults.Length; i++)
      {
        var actual = actualResults[i];
        var expected = expectedResults[i];

        if (expected.Line == -1 && expected.Column == -1)
        {
          if (actual.Location != Location.None)
          {
            Assert.IsTrue(false,
              $"Expected:\nA project diagnostic with No location\nActual:\n{FormatDiagnostics(analyzer, actual)}");
          }
        }
        else
        {
          VerifyDiagnosticLocation(analyzer, actual, actual.Location, expected.Locations.First());
          var additionalLocations = actual.AdditionalLocations.ToArray();

          if (additionalLocations.Length != expected.Locations.Length - 1)
          {
            Assert.IsTrue(false,
              $"Expected {expected.Locations.Length - 1} additional locations but got {additionalLocations.Length} for Diagnostic:\r\n    {FormatDiagnostics(analyzer, actual)}\r\n");
          }

          for (var j = 0; j < additionalLocations.Length; ++j)
          {
            VerifyDiagnosticLocation(analyzer, actual, additionalLocations[j], expected.Locations[j + 1]);
          }
        }

        if (actual.Id != expected.Id)
        {
          Assert.IsTrue(false,
            $"Expected diagnostic id to be \"{expected.Id}\" was \"{actual.Id}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer, actual)}\r\n");
        }

        if (actual.Severity != expected.Severity)
        {
          Assert.IsTrue(false,
            $"Expected diagnostic severity to be \"{expected.Severity}\" was \"{actual.Severity}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer, actual)}\r\n");
        }

        if (actual.GetMessage() != expected.Message)
        {
          Assert.IsTrue(false,
            $"Expected diagnostic message to be \"{expected.Message}\" was \"{actual.GetMessage()}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer, actual)}\r\n");
        }
      }
    }

    /// <summary>
    /// Helper method to VerifyDiagnosticResult that checks the location of a diagnostic and compares it with the location in the expected DiagnosticResult.
    /// </summary>
    /// <param name="analyzer">The analyzer that was being run on the sources</param>
    /// <param name="diagnostic">The diagnostic that was found in the code</param>
    /// <param name="actual">The Location of the Diagnostic found in the code</param>
    /// <param name="expected">The DiagnosticResultLocation that should have been found</param>
    private static void VerifyDiagnosticLocation(DiagnosticAnalyzer analyzer, Diagnostic diagnostic, Location actual, DiagnosticResultLocation expected)
    {
      var actualSpan = actual.GetLineSpan();

      Assert.IsTrue(actualSpan.Path == expected.Path || (actualSpan.Path != null && actualSpan.Path.Contains("Test0.") && expected.Path.Contains("Test.")),
        $"Expected diagnostic to be in file \"{expected.Path}\" was actually in file \"{actualSpan.Path}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer, diagnostic)}\r\n");

      var actualLinePosition = actualSpan.StartLinePosition;

      // Only check line position if there is an actual line in the real diagnostic
      if (actualLinePosition.Line > 0)
      {
        if (actualLinePosition.Line + 1 != expected.Line)
        {
          Assert.IsTrue(false,
            $"Expected diagnostic to be on line \"{expected.Line}\" was actually on line \"{actualLinePosition.Line + 1}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer, diagnostic)}\r\n");
        }
      }

      // Only check column position if there is an actual column position in the real diagnostic
      if (actualLinePosition.Character > 0)
      {
        if (actualLinePosition.Character + 1 != expected.Column)
        {
          Assert.IsTrue(false,
            $"Expected diagnostic to start at column \"{expected.Column}\" was actually at column \"{actualLinePosition.Character + 1}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer, diagnostic)}\r\n");
        }
      }
    }
    #endregion

    #region Formatting Diagnostics
    /// <summary>
    /// Helper method to format a Diagnostic into an easily readable string
    /// </summary>
    /// <param name="analyzer">The analyzer that this verifier tests</param>
    /// <param name="diagnostics">The Diagnostics to be formatted</param>
    /// <returns>The Diagnostics formatted as a string</returns>
    private static string FormatDiagnostics(DiagnosticAnalyzer analyzer, params Diagnostic[] diagnostics)
    {
      var builder = new StringBuilder();
      for (var i = 0; i < diagnostics.Length; ++i)
      {
        builder.AppendLine($"// {diagnostics[i]}");

        var analyzerType = analyzer.GetType();
        var rules = analyzer.SupportedDiagnostics;

        foreach (var rule in rules)
        {
          if (rule != null && rule.Id == diagnostics[i].Id)
          {
            var location = diagnostics[i].Location;
            if (location == Location.None)
            {
              builder.AppendFormat("GetGlobalResult({0}.{1})", analyzerType.Name, rule.Id);
            }
            else
            {
              Assert.IsTrue(location.IsInSource,
                  $"Test base does not currently handle diagnostics in metadata locations. Diagnostic in metadata: {diagnostics[i]}\r\n");

              var resultMethodName = diagnostics[i].Location.SourceTree.FilePath.EndsWith(".cs") ? "GetCSharpResultAt" : "GetBasicResultAt";
              var linePosition = diagnostics[i].Location.GetLineSpan().StartLinePosition;

              builder.AppendFormat("{0}({1}, {2}, {3}.{4})",
                  resultMethodName,
                  linePosition.Line + 1,
                  linePosition.Character + 1,
                  analyzerType.Name,
                  rule.Id);
            }

            if (i != diagnostics.Length - 1)
            {
              builder.Append(',');
            }

            builder.AppendLine();
            break;
          }
        }
      }
      return builder.ToString();
    }
    #endregion
  }
}
