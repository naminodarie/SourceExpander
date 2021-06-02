using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using SourceExpander.Roslyn;

namespace SourceExpander
{
    internal class DiagnosticContainer : IDiagnosticReporter
    {
        private readonly List<Diagnostic> _diagnostics = new();
        public DiagnosticContainer() { }
        public void ReportDiagnostic(Diagnostic diagnostic) => _diagnostics.Add(diagnostic);
    }
}
