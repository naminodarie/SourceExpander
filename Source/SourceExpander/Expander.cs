﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SourceExpander
{
    /// <summary>
    /// Expands embedded source codes.
    /// </summary>
    public static class Expander
    {
        /// <summary>
        /// <para>expand library code and comine expanded code and <paramref name="inputFilePath"/>.</para>
        /// <para>write combined code to <paramref name="outputFilePath"/>.</para>
        /// <para>if <paramref name="outputFilePath"/> is null, write the code to Combined.csx in <paramref name="inputFilePath"/>'s directory.</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Expand([CallerFilePath] string inputFilePath = null, string outputFilePath = null, bool ignoreAnyError = true)
        {
            try
            {
                if (inputFilePath is null)
                    throw new ArgumentNullException(nameof(inputFilePath));

                var combinedCode = ExpandString(inputFilePath, Assembly.GetCallingAssembly());
                if (outputFilePath is null)
                {
                    var directoryName = Path.GetDirectoryName(inputFilePath);
                    outputFilePath = Path.Combine(directoryName, "Combined.csx");
                }

                File.WriteAllText(outputFilePath, combinedCode);
            }
            catch (ExpanderException)
            {
                throw;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                if (!ignoreAnyError)
                    throw;
            }
        }

        /// <summary>
        /// expand library code and comine expanded code and <paramref name="inputFilePath"/>
        /// </summary>
        /// <returns>combined code</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string ExpandString([CallerFilePath] string inputFilePath = null, bool ignoreAnyError = true)
        {
            try
            {
                return ExpandString(inputFilePath, Assembly.GetCallingAssembly());
            }
            catch (ExpanderException)
            {
                throw;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                if (!ignoreAnyError)
                    throw;
                return "";
            }
        }

        private class ExpanderException : Exception
        {
            public ExpanderException() { }
            public ExpanderException(string message) : base(message) { }
            public ExpanderException(string message, Exception inner) : base(message, inner) { }
        }
        private static string ExpandString(string inputFilePath, Assembly callingAssembly)
        {
            if (inputFilePath is null)
                throw new ArgumentNullException(nameof(inputFilePath));

            var metadatas = callingAssembly.GetCustomAttributes<AssemblyMetadataAttribute>();
            if (metadatas.Any(m => m.Key == "SourceExpander.EmbedderVersion"))
            {
                throw new ExpanderException(Resources.HasEmbeddedSource);
            }

            var expandedContainerType = callingAssembly.GetType("SourceExpander.Expanded.ExpandedContainer");
            if (expandedContainerType is null)
                throw new InvalidOperationException("Needs SourceExpander.Generator");
            if (expandedContainerType.GetProperty("Files").GetValue(null)
                is not IReadOnlyDictionary<string, Expanded.SourceCode> dic)
                throw new InvalidOperationException("SourceExpander.Expanded.ExpandedContainer.Files is invalid");

            foreach (var sourceCode in dic.Values)
            {
                if (StringComparer.OrdinalIgnoreCase.Compare(
                    Path.GetFullPath(inputFilePath), Path.GetFullPath(sourceCode.Path)) == 0)
                {
                    return sourceCode.Code;
                }
            }
            throw new FileNotFoundException("input file must be in project.", inputFilePath);
        }
    }
}
