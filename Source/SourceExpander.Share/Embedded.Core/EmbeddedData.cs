﻿using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;

namespace SourceExpander
{
    public class EmbeddedData
    {
        public string AssemblyName { get; }
        public Version EmbedderVersion { get; }
        public LanguageVersion CSharpVersion { get; }
        public IReadOnlyList<SourceFileInfo> Sources { get; }
        public bool AllowUnsafe { get; }
        public bool IsEmpty => Sources.Count == 0;
        internal EmbeddedData(string assemblyName, IReadOnlyList<SourceFileInfo> sources,
            Version embedderVersion,
            LanguageVersion csharpVersion,
            bool allowUnsafe)
        {
            AssemblyName = assemblyName;
            Sources = sources;
            EmbedderVersion = embedderVersion;
            CSharpVersion = csharpVersion;
            AllowUnsafe = allowUnsafe;
        }
        public static EmbeddedData Create(string assemblyName, IEnumerable<KeyValuePair<string, string>> assemblyMetadatas)
        {
            LanguageVersion csharpVersion = LanguageVersion.CSharp1;
            Version? version = new Version(1, 0, 0);
            bool allowUnsafe = false;
            var list = new List<SourceFileInfo>();
            foreach (var pair in assemblyMetadatas)
            {
                var keyArray = pair.Key.Split('.');
                if (keyArray.Length < 2 || keyArray[0] != "SourceExpander")
                    continue;

                if (TryAddSourceFileInfos(keyArray, pair.Value, list)) { }
                else if (TryGetEmbedderVersion(keyArray, pair.Value, out var attrVersion))
                    version = attrVersion;
                else if (TryGetEmbeddedLanguageVersion(keyArray, pair.Value, out var attrCSharpVersion))
                    csharpVersion = attrCSharpVersion;
                else if (TryGetEmbeddedAllowUnsafe(keyArray, pair.Value, out var attrAllowUnsafe))
                    allowUnsafe = attrAllowUnsafe;
            }
            return new EmbeddedData(assemblyName, list, version, csharpVersion, allowUnsafe);
        }
        private static bool TryAddSourceFileInfos(string[] keyArray, string value, List<SourceFileInfo> list)
        {
            if (keyArray.Length >= 2
                && keyArray[1] == "EmbeddedSourceCode")
            {
                List<SourceFileInfo> embedded;
                if (Array.IndexOf(keyArray, "GZipBase32768", 2) >= 0)
                    embedded = SourceFileInfoUtil.ParseEmbeddedJson(SourceFileInfoUtil.FromGZipBase32768ToStream(value));
                else
                    embedded = SourceFileInfoUtil.ParseEmbeddedJson(value);
                list.AddRange(embedded);
                return true;
            }
            return false;
        }
        private static bool TryGetEmbedderVersion(string[] keyArray, string value, out Version? version)
        {
            if (keyArray.Length == 2
                && keyArray[1] == "EmbedderVersion"
                && Version.TryParse(value, out var embedderVersion))
            {
                version = embedderVersion;
                return true;
            }
            version = null;
            return false;
        }
        private static bool TryGetEmbeddedLanguageVersion(string[] keyArray, string value, out LanguageVersion version)
        {
            if (keyArray.Length == 2
                && keyArray[1] == "EmbeddedLanguageVersion"
                && LanguageVersionFacts.TryParse(value, out var embeddedVersion))
            {
                version = embeddedVersion;
                return true;
            }
            version = LanguageVersion.Default;
            return false;
        }
        private static bool TryGetEmbeddedAllowUnsafe(string[] keyArray, string value, out bool allowUnsafe)
        {
            if (keyArray.Length == 2
                && keyArray[1] == "EmbeddedAllowUnsafe"
                && bool.TryParse(value, out var embeddedAllowUnsafe))
            {
                allowUnsafe = embeddedAllowUnsafe;
                return true;
            }
            allowUnsafe = false;
            return false;
        }
    }
}