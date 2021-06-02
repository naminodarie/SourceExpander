using System;
using System.Reflection;

namespace SourceExpander
{
    internal class AssemblyUtil
    {
        public static readonly Version? AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
    }
}
