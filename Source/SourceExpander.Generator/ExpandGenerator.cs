﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using SourceExpander.Expanders;

namespace SourceExpander
{
    [Generator]
    public class ExpandGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            var (compilation, embeddeds) = Build((CSharpCompilation)context.Compilation);
            if (embeddeds.Length == 0)
            {
                var diagnosticDescriptor = new DiagnosticDescriptor("EXPAND0001", "not found embedded source", "not found embedded source", "ExpandGenerator", DiagnosticSeverity.Info, true);
                context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, Location.None));
            }

            context.AddSource("SourceExpander.Expanded.cs",
                SourceText.From(
                    MakeExpanded(compilation.SyntaxTrees.OfType<CSharpSyntaxTree>(), compilation, embeddeds),
                    Encoding.UTF8));
        }
        static (CSharpCompilation, SourceFileInfo[]) Build(CSharpCompilation compilation)
        {
            var trees = compilation.SyntaxTrees;
            foreach (var tree in trees)
            {
                var opts = tree.Options.WithDocumentationMode(DocumentationMode.Diagnose);
                compilation = compilation.ReplaceSyntaxTree(tree, tree.WithRootAndOptions(tree.GetRoot(), opts));
            }

            return (compilation, SourceFileInfoUtil.GetEmbeddedSourceFiles(compilation));
        }
        static string MakeExpanded(IEnumerable<CSharpSyntaxTree> trees, CSharpCompilation compilation, SourceFileInfo[] infos)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("namespace Expanded{");
            sb.AppendLine("public static class Expanded{");
            sb.AppendLine("public static IReadOnlyDictionary<string, string> Files { get; } = new Dictionary<string, string>{");
            foreach (var tree in trees)
            {
                var newCode = new CompilationExpander(tree, compilation, new SourceFileContainer(infos)).ExpandedString();
                sb.AppendLine($"{{{Quote(tree.FilePath)}, {Quote(newCode)}}},");
            }
            sb.AppendLine("};");
            sb.AppendLine("}}");

            return sb.ToString();
        }
        static string Quote(string str) => $"@\"{str.Replace("\"", "\"\"")}\"";
  
        public void Initialize(GeneratorInitializationContext context) { }
    }
}
