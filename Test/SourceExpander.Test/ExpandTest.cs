﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SourceExpander.Expanded;
using Xunit;

namespace SourceExpander.Test
{
    public class ExpandTest
    {
        [Fact]
        public void AssemblyMetadata()
        {
            var embedded = typeof(Expander).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .Where(attr => attr.Key.StartsWith("SourceExpander"))
                .ToDictionary(attr => attr.Key, attr => attr.Value);
            embedded.Should()
                .ContainKey("SourceExpander.EmbeddedLanguageVersion")
                .WhichValue.Should().Be("4");
            embedded.Should()
                .ContainKey("SourceExpander.EmbeddedSourceCode.GZipBase32768");
        }

        [Fact]
        public void Expand()
        {
            const string code = @"using System;
using SourceExpander;

class Program
{
    static void Main()
    {
        Console.WriteLine(42);
        Expander.Expand();
    }
}";
            var syntaxTrees = new[]
            {
                CSharpSyntaxTree.ParseText(
                    code,
                    options: new CSharpParseOptions(documentationMode:DocumentationMode.None)
                        .WithLanguageVersion(LanguageVersion.CSharp4),
                    path: "/home/source/Program.cs"),
            };

            var compilation = CSharpCompilation.Create(
                assemblyName: "TestAssembly",
                syntaxTrees: syntaxTrees,
                references: TestUtil.DefaulMetadatas,
                options: new CSharpCompilationOptions(OutputKind.ConsoleApplication)
                .WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic> {
                    { "CS8019", ReportDiagnostic.Suppress },
                }));
            compilation.SyntaxTrees.
                Should().HaveCount(syntaxTrees.Length);
            compilation.GetDiagnostics().Should().BeEmpty();

            var generator = new ExpandGenerator();
            var driver = CSharpGeneratorDriver.Create(new[] { generator },
                parseOptions:
                new CSharpParseOptions(kind: SourceCodeKind.Regular, documentationMode: DocumentationMode.Parse)
                    .WithLanguageVersion(LanguageVersion.CSharp4)
                );
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
            outputCompilation.GetDiagnostics().Should().BeEmpty();
            outputCompilation.SyntaxTrees.Should().HaveCount(syntaxTrees.Length + 1);

            outputCompilation.SyntaxTrees
            .Should()
            .ContainSingle(tree => tree.FilePath.EndsWith("SourceExpander.Expanded.cs"));
            var files = TestUtil.GetExpandedFilesWithCore(outputCompilation);
            files.Should().HaveCount(1);
            files["/home/source/Program.cs"].Should()
                .BeEquivalentTo(
                new SourceCode(
                    path: "/home/source/Program.cs",
                    code: @"using SourceExpander;
using System;
using System.Diagnostics;
class Program
{
    static void Main()
    {
        Console.WriteLine(42);
        Expander.Expand();
    }
}
#region Expanded
namespace SourceExpander { public class Expander { [Conditional(""EXPANDER"")] public static void Expand(string inputFilePath = null, string outputFilePath = null, bool ignoreAnyError = true) { } public static string ExpandString(string inputFilePath = null, bool ignoreAnyError = true) { return """"; } } } 
#endregion Expanded
")
                );
        }
    }
}
