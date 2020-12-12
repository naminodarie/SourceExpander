﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace SourceExpander
{
    public class ExpandConfig
    {
        public ExpandConfig()
            : this(
                  true,
                  Array.Empty<string>(),
                  Array.Empty<Regex>())
        { }
        public ExpandConfig(
            bool enabled,
            string[] matchFilePatterns,
            IEnumerable<Regex> ignoreFilePatterns)
        {
            Enabled = enabled;
            MatchFilePatterns = ImmutableArray.Create(matchFilePatterns);
            IgnoreFilePatterns = ImmutableArray.CreateRange(ignoreFilePatterns);
        }

        public bool Enabled { get; }
        public ImmutableArray<string> MatchFilePatterns { get; }
        public ImmutableArray<Regex> IgnoreFilePatterns { get; }
        public bool IsMatch(string filePath)
            => (MatchFilePatterns.Length == 0
                || MatchFilePatterns.Any(p => filePath.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0))
                && IgnoreFilePatterns.All(regex => !regex.IsMatch(filePath));

        public static ExpandConfig Parse(SourceText? sourceText, CancellationToken cancellationToken)
        {
            try
            {
                if (sourceText is not null && JsonUtil.ParseJson<ExpandConfigData>(sourceText, cancellationToken) is { } data)
                    return new ExpandConfig(
                        enabled: data.Enabled ?? true,
                        matchFilePatterns: data.MatchFilePattern ?? Array.Empty<string>(),
                        ignoreFilePatterns: data.IgnoreFilePatternRegex?.Select(s => new Regex(s))
                        ?? Array.Empty<Regex>());
                return new ExpandConfig();
            }
            catch (Exception e)
            {
                throw new ParseConfigException(e);
            }
        }

        [DataContract]
        private class ExpandConfigData
        {
            [DataMember(Name = "enabled")]
            public bool? Enabled { set; get; }
            [DataMember(Name = "match-file-pattern")]
            public string[]? MatchFilePattern { set; get; }
            [DataMember(Name = "ignore-file-pattern-regex")]
            public string[]? IgnoreFilePatternRegex { set; get; }

            public ExtensionDataObject? ExtensionData { get; set; }
        }
    }

#pragma warning disable CA1032
    internal sealed class ParseConfigException : Exception
    {
        public ParseConfigException() { }
        public ParseConfigException(Exception inner) : base(inner.Message, inner) { }
    }
}
