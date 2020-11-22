﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis;
using SourceExpander.Roslyn;

namespace SourceExpander.Embedder.Test
{
    public class MockDiagnosticReporter : IDiagnosticReporter
    {
        private readonly List<Diagnostic> diagnostics = new List<Diagnostic>();
        public IReadOnlyCollection<Diagnostic> Diagnostics { get; }
        public MockDiagnosticReporter()
        {
            Diagnostics = new ReadOnlyCollection<Diagnostic>(diagnostics);
        }
        public void ReportDiagnostic(Diagnostic diagnostic) => diagnostics.Add(diagnostic);
    }
}
