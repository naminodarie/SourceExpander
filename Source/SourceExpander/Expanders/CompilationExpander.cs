﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceExpander.Expanders.Utils;

namespace SourceExpander.Expanders
{
    internal class CompilationExpander : Expander
    {
        public CompilationExpander(string code, SourceFileContainer sourceFileContainer)
            : base(code, sourceFileContainer) { }

        private SyntaxTree? _origTree;
        protected SyntaxTree OrigTree => _origTree ??= CSharpSyntaxTree.ParseText(OrigCode);
        private ReadOnlyCollection<string>? linesCache;
        public override IEnumerable<string> ExpandedLines()
        {
            IEnumerable<string> Impl()
            {
                var dllPathes = Directory.EnumerateFiles(Path.GetDirectoryName(typeof(object).Assembly.Location), "*.dll");
                var compilation = CSharpCompilation.Create("compilation",
                    syntaxTrees: SourceFileContainer.Select(s => CSharpSyntaxTree.ParseText(s.RestoredCode)).Append(OrigTree),
                    references: dllPathes.Select(p => MetadataReference.CreateFromFile(p)));
                var semanticModel = compilation.GetSemanticModel(OrigTree);

                var origRoot = OrigTree.GetRoot();
                var requiedFiles = SourceFileContainer.ResolveDependency(GetRequiredSources(semanticModel, origRoot));

                var newRoot = (CompilationUnitSyntax)(new MatchSyntaxRemover(semanticModel
                    .GetDiagnostics(null)
                    .Where(d => d.Id == "CS8019" || d.Id == "CS0105")
                    .Select(d => origRoot.FindNode(d.Location.SourceSpan))
                    .OfType<UsingDirectiveSyntax>())
                    .Visit(origRoot) ?? throw new InvalidOperationException());

                var usings = new HashSet<string>(newRoot.Usings.Select(u => u.ToString().Trim()));

                var remover = new UsingRemover();
                var newBody = remover.Visit(newRoot).ToString();

                usings.UnionWith(requiedFiles.SelectMany(s => s.Usings));


                var sortedUsings = ExpanderUtil.SortedUsings(usings);
                foreach (var u in sortedUsings)
                    yield return u;

                using var sr = new StringReader(newBody);
                var line = sr.ReadLine();
                while (line != null)
                {
                    yield return line;
                    line = sr.ReadLine();
                }

                yield return "#region Expanded";
                foreach (var body in requiedFiles.SelectMany(s => ExpanderUtil.ToLines(s.CodeBody)))
                    yield return body;
                yield return "#endregion Expanded";
            }
            return linesCache ??= Array.AsReadOnly(Impl().ToArray());
        }


        private IEnumerable<SourceFileInfo> GetRequiredSources(SemanticModel semanticModel, SyntaxNode root)
        {
            var typeNames = root.DescendantNodes()
                .Select(s => GetTypeNameFromSymbol(semanticModel.GetSymbolInfo(s).Symbol))
                .OfType<string>()
                .Distinct()
                .ToArray();
            return SourceFileContainer
                .Where(s => s.TypeNames.Intersect(typeNames).Any())
                .ToArray();
        }
        private static string? GetTypeNameFromSymbol(ISymbol? symbol)
        {
            if (symbol == null) return null;
            if (symbol is INamedTypeSymbol named)
            {
                return named.ConstructedFrom.ToDisplayString();
            }
            return symbol.ContainingType?.ConstructedFrom?.ToDisplayString() ?? symbol.ToDisplayString();
        }
    }
}