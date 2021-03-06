﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using SourceExpander.Roslyn;

namespace SourceExpander
{
    [Generator]
    public class ExpandGenerator : ISourceGenerator
    {
        private const string CONFIG_FILE_NAME = "SourceExpander.Generator.Config.json";
        public void Initialize(GeneratorInitializationContext context) { }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types")]
        public void Execute(GeneratorExecutionContext context)
        {
            try
            {
                if (context.Compilation is not CSharpCompilation compilation
                    || context.ParseOptions is not CSharpParseOptions opts)
                    return;

                if ((int)opts.LanguageVersion <= (int)LanguageVersion.CSharp3)
                {
                    context.ReportDiagnostic(
                        DiagnosticDescriptors.EXPAND0004_MustBeNewerThanCSharp3());
                    return;
                }

                var configFile = context.AdditionalFiles
                        .FirstOrDefault(a =>
                            StringComparer.OrdinalIgnoreCase.Compare(Path.GetFileName(a.Path), CONFIG_FILE_NAME) == 0);

                context.CancellationToken.ThrowIfCancellationRequested();
                ExpandConfig config;
                if (configFile?.GetText(context.CancellationToken)?.ToString() is { } configText)
                {
                    try
                    {
                        config = ExpandConfig.Parse(configText);
                    }
                    catch (ParseJsonException e)
                    {
                        context.ReportDiagnostic(
                            DiagnosticDescriptors.EXPAND0007_ParseConfigError(configFile.Path, e.Message));
                        return;
                    }
                }
                else config = new();

                if (!config.Enabled)
                    return;

                const string SourceExpander_Expanded_SourceCode = "SourceExpander.Expanded.SourceCode";
                if (compilation.GetTypeByMetadataName(SourceExpander_Expanded_SourceCode) is null)
                {
                    context.AddSource("SourceExpander.SourceCode.cs",
                       SourceText.From(EmbeddingCore.SourceCodeClassCode, Encoding.UTF8));
                }

                context.CancellationToken.ThrowIfCancellationRequested();
                var loader = new EmbeddedLoader(compilation, opts, new DiagnosticReporter(context), config, context.CancellationToken);
                if (loader.IsEmbeddedEmpty)
                    context.ReportDiagnostic(DiagnosticDescriptors.EXPAND0003_NotFoundEmbedded());

                if (config.MetadataExpandingFile is { Length: > 0 } metadataExpandingFile)
                {
                    try
                    {
                        var (_, code) = loader.ExpandedCodes()
                           .First(t => t.filePath.IndexOf(metadataExpandingFile, StringComparison.OrdinalIgnoreCase) >= 0);

                        context.AddSource("SourceExpander.Metadata.cs",
                            CreateMetadataSource(new (string name, string code)[] {
                                ("SourceExpander.Expanded.Default", code),
                            }));
                    }
                    catch (InvalidOperationException)
                    {
                        context.ReportDiagnostic(DiagnosticDescriptors.EXPAND0009_MetadataEmbeddingFileNotFound(metadataExpandingFile));
                    }
                }
                var expandedCode = CreateExpanded(loader.ExpandedCodes());

                context.CancellationToken.ThrowIfCancellationRequested();
                context.AddSource("SourceExpander.Expanded.cs", expandedCode);
            }
            catch (OperationCanceledException)
            {
                Trace.WriteLine(nameof(ExpandGenerator) + "." + nameof(Execute) + "is Canceled.");
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.ToString());
                context.ReportDiagnostic(
                    DiagnosticDescriptors.EXPAND0001_UnknownError(e.Message));
            }
        }

        static SourceText CreateExpanded(IEnumerable<(string filePath, string expandedCode)> expanded)
        {
            static void CreateSourceCodeLiteral(StringBuilder sb, string pathLiteral, string codeLiteral)
                => sb.Append("SourceCode.FromDictionary(new Dictionary<string,object>{")
                  .AppendDicElement("\"path\"", pathLiteral)
                  .AppendDicElement("\"code\"", codeLiteral)
                  .Append("})");

            var sb = new StringBuilder();
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("namespace SourceExpander.Expanded{");
            sb.AppendLine("public static class ExpandedContainer{");
            sb.AppendLine("public static IReadOnlyDictionary<string, SourceCode> Files {get{ return _Files; }}");
            sb.AppendLine("private static Dictionary<string, SourceCode> _Files = new Dictionary<string, SourceCode>{");
            foreach (var (path, code) in expanded)
            {
                var filePathLiteral = path.ToLiteral();
                sb.AppendDicElement(filePathLiteral, sb => CreateSourceCodeLiteral(sb, filePathLiteral, code.ToLiteral()));
                sb.AppendLine();
            }
            sb.AppendLine("};");
            sb.AppendLine("}}");
            return SourceText.From(sb.ToString(), Encoding.UTF8);
        }

        static SourceText CreateMetadataSource(IEnumerable<(string Key, string Value)> metadatas)
        {
            StringBuilder sb = new();
            sb.AppendLine("using System.Reflection;");
            foreach (var (key, value) in metadatas)
            {
                sb.Append("[assembly: AssemblyMetadataAttribute(")
                  .Append(key.ToLiteral()).Append(",")
                  .Append(value.ToLiteral())
                  .AppendLine(")]");
            }
            return SourceText.From(sb.ToString(), Encoding.UTF8);
        }
    }
}
