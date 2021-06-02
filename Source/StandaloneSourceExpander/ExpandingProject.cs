using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

namespace SourceExpander
{
    public class ExpandingProject
    {
        public ExpandConfig Config { get; }

        public string CsprojPath { get; }

        private CancellationTokenSource CancellationTokenSource { get; }
        private CancellationToken CancellationToken => CancellationTokenSource.Token;

        public ExpandingProject(string csprojPath) : this(csprojPath, new ExpandConfig()) { }
        public ExpandingProject(string csprojPath, ExpandConfig config)
        {
            CancellationTokenSource = new CancellationTokenSource();
            Config = config;
            CsprojPath = csprojPath;
        }
        public void Cancel() => CancellationTokenSource.Cancel();

        public async Task<ImmutableArray<(string filePath, string expandedCode)>> ExpandAsync()
        {
            if (MSBuildLocator.CanRegister)
                MSBuildLocator.RegisterDefaults();
            using var workspace = MSBuildWorkspace.Create();
            var project = await workspace.OpenProjectAsync(CsprojPath, cancellationToken: CancellationToken);
            var compilationAny = await project.GetCompilationAsync();
            if (compilationAny is not CSharpCompilation compilation || !compilation.SyntaxTrees.Any())
                return ImmutableArray<(string filePath, string expandedCode)>.Empty;
            var diags = compilation.GetDiagnostics();

            var parseOptions = (CSharpParseOptions)project.ParseOptions!;
            var diagnosticContainer = new DiagnosticContainer();
            return new EmbeddedLoader(
                compilation,
                parseOptions,
                diagnosticContainer,
                Config,
                CancellationToken).ExpandedCodes();
        }
    }
}
