﻿using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace SourceExpander.Generate
{
    public class ConfigTest : ExpandGeneratorTestBase
    {
        public static readonly TheoryData ParseErrorJsons = new TheoryData<InMemorySourceText, object[]>
        {
            {
                new InMemorySourceText("/foo/bar/SourceExpander.Generator.Config.json", @"
{
    ""$schema"": ""https://raw.githubusercontent.com/naminodarie/SourceExpander/master/schema/expander.schema.json"",
    ""ignore-file-pattern-regex"": 1
}
"),
                new object[]
                {
                    "/foo/bar/SourceExpander.Generator.Config.json",
                    "Error converting value 1 to type 'System.String[]'. Path 'ignore-file-pattern-regex', line 4, position 34."
                }
            },
            {
                new InMemorySourceText("/foo/bar/sourceExpander.generator.config.json", @"
{
    ""$schema"": ""https://raw.githubusercontent.com/naminodarie/SourceExpander/master/schema/expander.schema.json"",
    ""ignore-file-pattern-regex"": 1
}
"),
                new object[]
                {
                    "/foo/bar/sourceExpander.generator.config.json",
                    "Error converting value 1 to type 'System.String[]'. Path 'ignore-file-pattern-regex', line 4, position 34."
                }
            },
            {
                new InMemorySourceText("/regexerror/SourceExpander.Generator.Config.json", @"
{
    ""$schema"": ""https://raw.githubusercontent.com/naminodarie/SourceExpander/master/schema/expander.schema.json"",
    ""ignore-file-pattern-regex"": [
        ""(""
    ]
}
"),
                new object[]
                {
                    "/regexerror/SourceExpander.Generator.Config.json",
                    "Invalid pattern '(' at offset 1. Not enough )'s."
                }
            },
        };

        [Theory]
        [MemberData(nameof(ParseErrorJsons))]
        public async Task ParseErrorTest(InMemorySourceText additionalText, object[] diagnosticsArg)
        {
            var others = new SourceFileCollection{
                (
                @"/home/other/C.cs",
                "namespace Other{public static class C{public static void P()=>System.Console.WriteLine();}}"
                ),
                (
                @"/home/other/AssemblyInfo.cs",
                @"[assembly: System.Reflection.AssemblyMetadata(""SourceExpander.EmbeddedSourceCode"", ""[{\""CodeBody\"":\""namespace Other { public static class C { public static void P() => System.Console.WriteLine(); } } \"",\""Dependencies\"":[],\""FileName\"":\""OtherDependency>C.cs\"",\""TypeNames\"":[\""Other.C\""],\""Usings\"":[]}]"")]"
                + @"[assembly: System.Reflection.AssemblyMetadata(""SourceExpander.EmbedderVersion"",""1.1.1.1"")]"
                + @"[assembly: System.Reflection.AssemblyMetadata(""SourceExpander.EmbeddedLanguageVersion"",""7.2"")]"
                ),
            };

            var test = new Test
            {
                SolutionTransforms =
                {
                    (solution, projectId)
                    => CreateOtherReference(solution, projectId, others),
                },
                TestState =
                {
                    AdditionalFiles = { additionalText, },
                    Sources = {
                        (
                            @"/home/mine/Program.cs",
                            @"using System;
using Other;

class Program
{
    static void Main()
    {
        Console.WriteLine(42);
        C.P();
    }
}
"
                        ),
                        (
                            @"/home/mine/Program2.cs",
                            @"using System;
using Other;

class Program2
{
    static void M()
    {
        C.P();
    }
}
"
                        ),
                    },
                    ExpectedDiagnostics =
                    {
                        DiagnosticResult.CompilerError("EXPAND0007")
                            .WithSpan(additionalText.Path, 1, 1, 1, 1)
                            .WithArguments(diagnosticsArg),
                    },
                }
            };
            await test.RunAsync();
        }

        [Fact]
        public async Task NotEnabled()
        {
            var others = new SourceFileCollection{
                (
                @"/home/other/C.cs",
                "namespace Other{public static class C{public static void P()=>System.Console.WriteLine();}}"
                ),
                (
                @"/home/other/AssemblyInfo.cs",
                @"[assembly: System.Reflection.AssemblyMetadata(""SourceExpander.EmbeddedSourceCode"", ""[{\""CodeBody\"":\""namespace Other { public static class C { public static void P() => System.Console.WriteLine(); } } \"",\""Dependencies\"":[],\""FileName\"":\""OtherDependency>C.cs\"",\""TypeNames\"":[\""Other.C\""],\""Usings\"":[]}]"")]"
                + @"[assembly: System.Reflection.AssemblyMetadata(""SourceExpander.EmbedderVersion"",""1.1.1.1"")]"
                + @"[assembly: System.Reflection.AssemblyMetadata(""SourceExpander.EmbeddedLanguageVersion"",""7.2"")]"
                ),
            };
            var additionalText = new InMemorySourceText(
                "/foo/bar/SourceExpander.Generator.Config.json", @"
{
    ""$schema"": ""https://raw.githubusercontent.com/naminodarie/SourceExpander/master/schema/expander.schema.json"",
    ""enabled"": false
}
");
            var test = new Test
            {
                SolutionTransforms =
                {
                    (solution, projectId)
                    => CreateOtherReference(solution, projectId, others),
                },
                TestState =
                {
                    AdditionalFiles = { additionalText, },
                    Sources = {
                        (
                            @"/home/mine/Program.cs",
                            @"using System;
using Other;

class Program
{
    static void Main()
    {
        Console.WriteLine(42);
        C.P();
    }
}
"
                        ),
                        (
                            @"/home/mine/Program2.cs",
                            @"using System;
using Other;

class Program2
{
    static void M()
    {
        C.P();
    }
}
"
                        ),
                    },
                    ExpectedDiagnostics =
                    {
                    },
                }
            };
            await test.RunAsync();
        }

        [Fact]
        public async Task IgnoreFile()
        {
            var others = new SourceFileCollection{
                (
                @"/home/other/C.cs",
                "namespace Other{public static class C{public static void P()=>System.Console.WriteLine();}}"
                ),
                (
                @"/home/other/AssemblyInfo.cs",
                @"[assembly: System.Reflection.AssemblyMetadata(""SourceExpander.EmbeddedSourceCode"", ""[{\""CodeBody\"":\""namespace Other { public static class C { public static void P() => System.Console.WriteLine(); } } \"",\""Dependencies\"":[],\""FileName\"":\""OtherDependency>C.cs\"",\""TypeNames\"":[\""Other.C\""],\""Usings\"":[]}]"")]"
                + @"[assembly: System.Reflection.AssemblyMetadata(""SourceExpander.EmbedderVersion"",""1.1.1.1"")]"
                + @"[assembly: System.Reflection.AssemblyMetadata(""SourceExpander.EmbeddedLanguageVersion"",""7.2"")]"
                ),
            };


            // language=regex
            var pattern = @"mine/Program\d+\.cs$";
            var additionalText = new InMemorySourceText(
                "/foo/bar/SourceExpander.Generator.Config.json", @"
{
    ""$schema"": ""https://raw.githubusercontent.com/naminodarie/SourceExpander/master/schema/expander.schema.json"",
    ""ignore-file-pattern-regex"": [
" + pattern.ToLiteral() + @"
    ]
}
");

            var test = new Test
            {
                SolutionTransforms =
                {
                    (solution, projectId)
                    => CreateOtherReference(solution, projectId, others),
                },
                TestState =
                {
                    AdditionalFiles = { additionalText, },
                    Sources = {
                        (
                            @"/home/mine/Program.cs",
                            @"using System;
using Other;

class Program
{
    static void Main()
    {
        Console.WriteLine(42);
        C.P();
    }
}
"
                        ),
                        (
                            @"/home/mine/X/Program2.cs",
                            @"using System;
using Other;

class Program2
{
    static void M()
    {
        C.P();
    }
}
"
                        ),
                        (
                            @"/home/mine/Program1.cs",
                            @"using System;
using Other;

class Program1
{
    static void M()
    {
        C.P();
    }
}
"
                        ),
                        (
                            @"/home/mine/Program3.cs",
                            @"using System;
using Other;

class Program3
{
    static void M()
    {
        C.P();
    }
}
"
                        ),
                    },
                    ExpectedDiagnostics =
                    {
                    },
                    GeneratedSources =
                    {
                        (typeof(ExpandGenerator), "SourceExpander.Expanded.cs", @"using System.Collections.Generic;
namespace SourceExpander.Expanded{
public static class ExpandedContainer{
public static IReadOnlyDictionary<string, SourceCode> Files {get{ return _Files; }}
private static Dictionary<string, SourceCode> _Files = new Dictionary<string, SourceCode>{
{""/home/mine/Program.cs"",SourceCode.FromDictionary(new Dictionary<string,object>{{""path"",""/home/mine/Program.cs""},{""code"",""using Other;\r\nusing System;\r\nclass Program\r\n{\r\n    static void Main()\r\n    {\r\n        Console.WriteLine(42);\r\n        C.P();\r\n    }\r\n}\r\n#region Expanded by https://github.com/naminodarie/SourceExpander\r\nnamespace Other { public static class C { public static void P() => System.Console.WriteLine(); } } \r\n#endregion Expanded by https://github.com/naminodarie/SourceExpander\r\n""},})},
{""/home/mine/X/Program2.cs"",SourceCode.FromDictionary(new Dictionary<string,object>{{""path"",""/home/mine/X/Program2.cs""},{""code"",""using Other;\r\nclass Program2\r\n{\r\n    static void M()\r\n    {\r\n        C.P();\r\n    }\r\n}\r\n#region Expanded by https://github.com/naminodarie/SourceExpander\r\nnamespace Other { public static class C { public static void P() => System.Console.WriteLine(); } } \r\n#endregion Expanded by https://github.com/naminodarie/SourceExpander\r\n""},})},
};
}}
".ReplaceEOL())
                    }
                }
            };
            await test.RunAsync();
        }

        [Fact]
        public async Task Whitelist()
        {
            var others = new SourceFileCollection{
                (
                @"/home/other/C.cs",
                "namespace Other{public static class C{public static void P()=>System.Console.WriteLine();}}"
                ),
                (
                @"/home/other/AssemblyInfo.cs",
                @"[assembly: System.Reflection.AssemblyMetadata(""SourceExpander.EmbeddedSourceCode"", ""[{\""CodeBody\"":\""namespace Other { public static class C { public static void P() => System.Console.WriteLine(); } } \"",\""Dependencies\"":[],\""FileName\"":\""OtherDependency>C.cs\"",\""TypeNames\"":[\""Other.C\""],\""Usings\"":[]}]"")]"
                + @"[assembly: System.Reflection.AssemblyMetadata(""SourceExpander.EmbedderVersion"",""1.1.1.1"")]"
                + @"[assembly: System.Reflection.AssemblyMetadata(""SourceExpander.EmbeddedLanguageVersion"",""7.2"")]"
                ),
            };
            var pattern = @"mine/Program.cs";
            var additionalText = new InMemorySourceText(
                "/foo/bar/SourceExpander.Generator.Config.json", @"
{
    ""$schema"": ""https://raw.githubusercontent.com/naminodarie/SourceExpander/master/schema/expander.schema.json"",
    ""match-file-pattern"": [
" + pattern.ToLiteral() + @"
    ]
}
");
            var test = new Test
            {
                SolutionTransforms =
                {
                    (solution, projectId)
                    => CreateOtherReference(solution, projectId, others),
                },
                TestState =
                {
                    AdditionalFiles = { additionalText, },
                    Sources = {
                        (
                            @"/home/mine/Program.cs",
                            @"using System;
using Other;

class Program
{
    static void Main()
    {
        Console.WriteLine(42);
        C.P();
    }
}
"
                        ),
                        (
                            @"/home/mine/Program2.cs",
                            @"using System;
using Other;

class Program2
{
    static void M()
    {
        C.P();
    }
}
"
                        ),
                    },
                    ExpectedDiagnostics =
                    {
                    },
                    GeneratedSources =
                    {
                        (typeof(ExpandGenerator), "SourceExpander.Expanded.cs", @"using System.Collections.Generic;
namespace SourceExpander.Expanded{
public static class ExpandedContainer{
public static IReadOnlyDictionary<string, SourceCode> Files {get{ return _Files; }}
private static Dictionary<string, SourceCode> _Files = new Dictionary<string, SourceCode>{
{""/home/mine/Program.cs"",SourceCode.FromDictionary(new Dictionary<string,object>{{""path"",""/home/mine/Program.cs""},{""code"",""using Other;\r\nusing System;\r\nclass Program\r\n{\r\n    static void Main()\r\n    {\r\n        Console.WriteLine(42);\r\n        C.P();\r\n    }\r\n}\r\n#region Expanded by https://github.com/naminodarie/SourceExpander\r\nnamespace Other { public static class C { public static void P() => System.Console.WriteLine(); } } \r\n#endregion Expanded by https://github.com/naminodarie/SourceExpander\r\n""},})},
};
}}
".ReplaceEOL())
                    }
                }
            };
            await test.RunAsync();
        }

        [Fact]
        public async Task StaticEmbeddingText()
        {
            var others = new SourceFileCollection{
                (
                @"/home/other/C.cs",
                "namespace Other{public static class C{public static void P()=>System.Console.WriteLine();}}"
                ),
                (
                @"/home/other/AssemblyInfo.cs",
                @"[assembly: System.Reflection.AssemblyMetadata(""SourceExpander.EmbeddedSourceCode"", ""[{\""CodeBody\"":\""namespace Other { public static class C { public static void P() => System.Console.WriteLine(); } } \"",\""Dependencies\"":[],\""FileName\"":\""OtherDependency>C.cs\"",\""TypeNames\"":[\""Other.C\""],\""Usings\"":[]}]"")]"
                + @"[assembly: System.Reflection.AssemblyMetadata(""SourceExpander.EmbedderVersion"",""1.1.1.1"")]"
                + @"[assembly: System.Reflection.AssemblyMetadata(""SourceExpander.EmbeddedLanguageVersion"",""7.2"")]"
                ),
            };

            var additionalText = new InMemorySourceText(
                "/foo/bar/SourceExpander.Generator.Config.json", @"
{
    ""$schema"": ""https://raw.githubusercontent.com/naminodarie/SourceExpander/master/schema/expander.schema.json"",
    ""static-embedding-text"": ""/* Static Embedding Text */""
}
");
            var test = new Test
            {
                SolutionTransforms =
                {
                    (solution, projectId)
                    => CreateOtherReference(solution, projectId, others),
                },
                TestState =
                {
                    AdditionalFiles = { additionalText },
                    Sources = {
                        (
                            @"/home/mine/Program.cs",
                            @"using System;
using Other;

class Program
{
    static void Main()
    {
        Console.WriteLine(42);
        C.P();
    }
}
"
                        ),
                        (
                            @"/home/mine/Program2.cs",
                            @"using System;
using Other;

class Program2
{
    static void M()
    {
        C.P();
    }
}
"
                        ),
                    },
                    ExpectedDiagnostics =
                    {
                    },
                    GeneratedSources =
                    {
                        (typeof(ExpandGenerator), "SourceExpander.Expanded.cs", @"using System.Collections.Generic;
namespace SourceExpander.Expanded{
public static class ExpandedContainer{
public static IReadOnlyDictionary<string, SourceCode> Files {get{ return _Files; }}
private static Dictionary<string, SourceCode> _Files = new Dictionary<string, SourceCode>{
{""/home/mine/Program.cs"",SourceCode.FromDictionary(new Dictionary<string,object>{{""path"",""/home/mine/Program.cs""},{""code"",""using Other;\r\nusing System;\r\nclass Program\r\n{\r\n    static void Main()\r\n    {\r\n        Console.WriteLine(42);\r\n        C.P();\r\n    }\r\n}\r\n#region Expanded by https://github.com/naminodarie/SourceExpander\r\n/* Static Embedding Text */\r\nnamespace Other { public static class C { public static void P() => System.Console.WriteLine(); } } \r\n#endregion Expanded by https://github.com/naminodarie/SourceExpander\r\n""},})},
{""/home/mine/Program2.cs"",SourceCode.FromDictionary(new Dictionary<string,object>{{""path"",""/home/mine/Program2.cs""},{""code"",""using Other;\r\nclass Program2\r\n{\r\n    static void M()\r\n    {\r\n        C.P();\r\n    }\r\n}\r\n#region Expanded by https://github.com/naminodarie/SourceExpander\r\n/* Static Embedding Text */\r\nnamespace Other { public static class C { public static void P() => System.Console.WriteLine(); } } \r\n#endregion Expanded by https://github.com/naminodarie/SourceExpander\r\n""},})},
};
}}
".ReplaceEOL())
                    }
                }
            };
            await test.RunAsync();
        }

        [Fact]
        public async Task MetadataExpandingFileNotFound()
        {
            var others = new SourceFileCollection{
                (
                @"/home/other/C.cs",
                "namespace Other{public static class C{public static void P()=>System.Console.WriteLine();}}"
                ),
                (
                @"/home/other/AssemblyInfo.cs",
                @"[assembly: System.Reflection.AssemblyMetadata(""SourceExpander.EmbeddedSourceCode"", ""[{\""CodeBody\"":\""namespace Other { public static class C { public static void P() => System.Console.WriteLine(); } } \"",\""Dependencies\"":[],\""FileName\"":\""OtherDependency>C.cs\"",\""TypeNames\"":[\""Other.C\""],\""Usings\"":[]}]"")]"
                + @"[assembly: System.Reflection.AssemblyMetadata(""SourceExpander.EmbedderVersion"",""1.1.1.1"")]"
                + @"[assembly: System.Reflection.AssemblyMetadata(""SourceExpander.EmbeddedLanguageVersion"",""7.2"")]"
                ),
            };

            var additionalText = new InMemorySourceText(
                "/foo/bar/SourceExpander.Generator.Config.json", @"
{
    ""$schema"": ""https://raw.githubusercontent.com/naminodarie/SourceExpander/master/schema/expander.schema.json"",
    ""metadata-expanding-file"": ""Program0.cs"",
    ""static-embedding-text"": ""/* Static Embedding Text */""
}
");
            var test = new Test
            {
                SolutionTransforms =
                {
                    (solution, projectId)
                    => CreateOtherReference(solution, projectId, others),
                },
                TestState =
                {
                    AdditionalFiles = { additionalText },
                    Sources = {
                        (
                            @"/home/mine/Program.cs",
                            @"using System;
using Other;

class Program
{
    static void Main()
    {
        Console.WriteLine(42);
        C.P();
    }
}
"
                        ),
                        (
                            @"/home/mine/Program2.cs",
                            @"using System;
using Other;

class Program2
{
    static void M()
    {
        C.P();
    }
}
"
                        ),
                    },
                    ExpectedDiagnostics =
                    {
                        DiagnosticResult.CompilerWarning("EXPAND0009").WithArguments("Program0.cs"),
                    },
                    GeneratedSources =
                    {
                        (typeof(ExpandGenerator), "SourceExpander.Expanded.cs", @"using System.Collections.Generic;
namespace SourceExpander.Expanded{
public static class ExpandedContainer{
public static IReadOnlyDictionary<string, SourceCode> Files {get{ return _Files; }}
private static Dictionary<string, SourceCode> _Files = new Dictionary<string, SourceCode>{
{""/home/mine/Program.cs"",SourceCode.FromDictionary(new Dictionary<string,object>{{""path"",""/home/mine/Program.cs""},{""code"",""using Other;\r\nusing System;\r\nclass Program\r\n{\r\n    static void Main()\r\n    {\r\n        Console.WriteLine(42);\r\n        C.P();\r\n    }\r\n}\r\n#region Expanded by https://github.com/naminodarie/SourceExpander\r\n/* Static Embedding Text */\r\nnamespace Other { public static class C { public static void P() => System.Console.WriteLine(); } } \r\n#endregion Expanded by https://github.com/naminodarie/SourceExpander\r\n""},})},
{""/home/mine/Program2.cs"",SourceCode.FromDictionary(new Dictionary<string,object>{{""path"",""/home/mine/Program2.cs""},{""code"",""using Other;\r\nclass Program2\r\n{\r\n    static void M()\r\n    {\r\n        C.P();\r\n    }\r\n}\r\n#region Expanded by https://github.com/naminodarie/SourceExpander\r\n/* Static Embedding Text */\r\nnamespace Other { public static class C { public static void P() => System.Console.WriteLine(); } } \r\n#endregion Expanded by https://github.com/naminodarie/SourceExpander\r\n""},})},
};
}}
".ReplaceEOL())
                    }
                }
            };
            await test.RunAsync();
        }

        [Fact]
        public async Task MetadataExpandingFile()
        {
            var others = new SourceFileCollection{
                (
                @"/home/other/C.cs",
                "namespace Other{public static class C{public static void P()=>System.Console.WriteLine();}}"
                ),
                (
                @"/home/other/AssemblyInfo.cs",
                @"[assembly: System.Reflection.AssemblyMetadata(""SourceExpander.EmbeddedSourceCode"", ""[{\""CodeBody\"":\""namespace Other { public static class C { public static void P() => System.Console.WriteLine(); } } \"",\""Dependencies\"":[],\""FileName\"":\""OtherDependency>C.cs\"",\""TypeNames\"":[\""Other.C\""],\""Usings\"":[]}]"")]"
                + @"[assembly: System.Reflection.AssemblyMetadata(""SourceExpander.EmbedderVersion"",""1.1.1.1"")]"
                + @"[assembly: System.Reflection.AssemblyMetadata(""SourceExpander.EmbeddedLanguageVersion"",""7.2"")]"
                ),
            };

            var additionalText = new InMemorySourceText(
                "/foo/bar/SourceExpander.Generator.Config.json", @"
{
    ""$schema"": ""https://raw.githubusercontent.com/naminodarie/SourceExpander/master/schema/expander.schema.json"",
    ""metadata-expanding-file"": ""Program.cs"",
    ""static-embedding-text"": ""/* Static Embedding Text */""
}
");
            var test = new Test
            {
                SolutionTransforms =
                {
                    (solution, projectId)
                    => CreateOtherReference(solution, projectId, others),
                },
                TestState =
                {
                    AdditionalFiles = { additionalText },
                    Sources = {
                        (
                            @"/home/mine/Program.cs",
                            @"using System;
using Other;

class Program
{
    static void Main()
    {
        Console.WriteLine(42);
        C.P();
    }
}
"
                        ),
                        (
                            @"/home/mine/Program2.cs",
                            @"using System;
using Other;

class Program2
{
    static void M()
    {
        C.P();
    }
}
"
                        ),
                    },
                    ExpectedDiagnostics =
                    {
                    },
                    GeneratedSources =
                    {
                        (typeof(ExpandGenerator), "SourceExpander.Metadata.cs", @"using System.Reflection;
[assembly: AssemblyMetadataAttribute(""SourceExpander.Expanded.Default"",""using Other;\r\nusing System;\r\nclass Program\r\n{\r\n    static void Main()\r\n    {\r\n        Console.WriteLine(42);\r\n        C.P();\r\n    }\r\n}\r\n#region Expanded by https://github.com/naminodarie/SourceExpander\r\n/* Static Embedding Text */\r\nnamespace Other { public static class C { public static void P() => System.Console.WriteLine(); } } \r\n#endregion Expanded by https://github.com/naminodarie/SourceExpander\r\n"")]
".ReplaceEOL()),
                        (typeof(ExpandGenerator), "SourceExpander.Expanded.cs", @"using System.Collections.Generic;
namespace SourceExpander.Expanded{
public static class ExpandedContainer{
public static IReadOnlyDictionary<string, SourceCode> Files {get{ return _Files; }}
private static Dictionary<string, SourceCode> _Files = new Dictionary<string, SourceCode>{
{""/home/mine/Program.cs"",SourceCode.FromDictionary(new Dictionary<string,object>{{""path"",""/home/mine/Program.cs""},{""code"",""using Other;\r\nusing System;\r\nclass Program\r\n{\r\n    static void Main()\r\n    {\r\n        Console.WriteLine(42);\r\n        C.P();\r\n    }\r\n}\r\n#region Expanded by https://github.com/naminodarie/SourceExpander\r\n/* Static Embedding Text */\r\nnamespace Other { public static class C { public static void P() => System.Console.WriteLine(); } } \r\n#endregion Expanded by https://github.com/naminodarie/SourceExpander\r\n""},})},
{""/home/mine/Program2.cs"",SourceCode.FromDictionary(new Dictionary<string,object>{{""path"",""/home/mine/Program2.cs""},{""code"",""using Other;\r\nclass Program2\r\n{\r\n    static void M()\r\n    {\r\n        C.P();\r\n    }\r\n}\r\n#region Expanded by https://github.com/naminodarie/SourceExpander\r\n/* Static Embedding Text */\r\nnamespace Other { public static class C { public static void P() => System.Console.WriteLine(); } } \r\n#endregion Expanded by https://github.com/naminodarie/SourceExpander\r\n""},})},
};
}}
".ReplaceEOL())
                    }
                }
            };
            await test.RunAsync();
        }

    }
}
