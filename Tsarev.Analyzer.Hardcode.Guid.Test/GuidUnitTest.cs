using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Tsarev.Analyzer.TestHelpers;
using Xunit;

namespace Tsarev.Analyzer.Hardcode.Guid.Test
{
  public class GuidUnitTest : DiagnosticVerifier
  {

    //No diagnostics expected to show up
    [Fact]
    public void TestEmpty()
    {
      var test = @"";

      VerifyCSharpDiagnostic(test);
    }

    //Diagnostic triggered and checked for
    [Fact]
    public void TestExpectedHardcodedGuidInCtor()
    {
      var test = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
           public void Test()
            {
               var test = new Guid(""0adcd044-c1fb-469b-b90c-9c7fdea18380"");
            }
        }
    }";

      VerifyCSharpDiagnostic(test, ExpectGuidHardcode(9, 27));
    }

    [Fact]
    public void TestExpectedHardcodedGuidInParse()
    {
      var test = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
           public void Test()
            {
               var test = Guid.Parse(""0adcd044-c1fb-469b-b90c-9c7fdea18380"");
            }
        }
    }";

      VerifyCSharpDiagnostic(test, ExpectGuidHardcode(9, 27));
    }

    [Fact]
    public void TestExpectedHardcodedGuidInParseExact()
    {
      var test = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
           public void Test()
            {
               var test = Guid.ParseExact(""0adcd044-c1fb-469b-b90c-9c7fdea18380"", ""N"");
            }
        }
    }";

      VerifyCSharpDiagnostic(test, ExpectGuidHardcode(9, 27));
    }

    [Fact]
    public void TestExpectedHardcodedGuidInTryParse() { 
      var test = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
           public void Test()
            { 
               Guid test;
               Guid.TryParse(""0adcd044-c1fb-469b-b90c-9c7fdea18380"", out test);
            }
        }
    }";

      VerifyCSharpDiagnostic(test, ExpectGuidHardcode(10, 16));
    }

    [Fact]
    public void TestExpectedHardcodedGuidInTryParseExact()
    {
      var test = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
           public void Test()
            {
               Guid test;
               Guid.TryParseExact(""0adcd044-c1fb-469b-b90c-9c7fdea18380"", ""N"", out test);
            }
        }
    }";

      VerifyCSharpDiagnostic(test, ExpectGuidHardcode(10, 16));
    }


    [Fact]
    public void TestExpectedHardcodedGuidInMember()
    {
      var test = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
           public static readonly Guid MyGuid = new Guid(""0adcd044-c1fb-469b-b90c-9c7fdea18380"");
        }
    }";

      VerifyCSharpDiagnostic(test, ExpectGuidHardcode(7, 49));
    }

    [Fact]
    public void TestIgnoredInEntity()
    {
      var test = @"
    using System;
    using System.Data.Entity;
    namespace ConsoleApplication1
    {
        class TypeName
        {
           public int Id {get;set;}
           public static readonly Guid MyGuid = new Guid(""0adcd044-c1fb-469b-b90c-9c7fdea18380"");
        }
    }";

      VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void TestIgnoredInSubclassOfEntity()
    {
      var test = @"
    using System;
    using System.Data.Entity;
    namespace ConsoleApplication1
    {
        class TypeName
        {
           public int Id {get;set;}
           public class Constants {
              public static readonly Guid MyGuid = new Guid(""0adcd044-c1fb-469b-b90c-9c7fdea18380"");
           }
        }
    }";

      VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void TestIgnoredVariable()
    {
      var test = @"
       using System;
       public class FooClass
             internal static Guid SendMessage(string url, string phoneNumber, string text)
        {
                string smsServiceId = GetSomeString();
                Guid result;
                if (!Guid.TryParse(smsServiceId, out result))
                    result = Guid.Empty;
                return result;
        }
       }";

      VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void TestIgnoreConfigLoad()
    {
      var test = @"
          
          using System;
          public static Guid Field => Guid.Parse(ConfigurationManager.AppSettings.Get(""ConfigVariableName""));";

      VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void TestRobustForConstructorWithoutArguments()
    {
      var test = @"
     using System;
     using System.Collections.Generic;
     public class FooClass {
        private IEnumerable<TransportInfo> GetTransports()
        {
            var list = new List<int> { 1 };
        }
      }
      ";
      VerifyCSharpDiagnostic(test);
    }


    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new GuidHardcodeAnalyzer();


    private DiagnosticResult ExpectGuidHardcode(int line, int column) => new DiagnosticResult
    {
      Id = nameof(GuidHardcodeAnalyzer),
      Message = "Attempt to create hardcoded Guid value",
      Severity = DiagnosticSeverity.Warning,
      Locations =
        new[]
        {
          new DiagnosticResultLocation("Test0.cs", line, column)
        }
    };
  }
}
