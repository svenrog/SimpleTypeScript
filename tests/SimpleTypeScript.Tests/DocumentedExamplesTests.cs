using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Text;
using Xunit;

namespace SimpleTypeScript.Tests;

/// <summary>
/// Every C# example in every README, compiled.
/// <para>
/// A README is the first thing a consumer copies and the last thing anybody edits, so an example that no
/// longer compiles is the most expensive kind of stale documentation — it is read as the API. Compiling them
/// is the only check that cannot itself go out of date: a renamed member or a changed signature fails here,
/// in the same build that renamed it.
/// </para>
/// </summary>
public sealed class DocumentedExamplesTests
{
    /// <summary>
    /// What the examples are allowed to assume exists: the usings a consumer would have, and stand-ins for
    /// the domain types a README names to make a point. Kept small on purpose — an example needing much more
    /// than this is one that has stopped illustrating the package.
    /// </summary>
    private const string _preamble = """
        using System;
        using System.Collections.Generic;
        using System.IO;
        using System.Linq;
        using System.Text.Json;
        using System.Text.Json.Serialization;
        using SimpleTypeScript;
        using SimpleTypeScript.TypeGeneration;
        using SimpleTypeScript.TypeGeneration.Documentation;
        using SimpleTypeScript.TypeGeneration.Modules;
        using Docs;

        namespace Docs
        {
            public sealed class Order
            {
                public Guid Id { get; init; }
            }

            public sealed class Customer
            {
                public string Name { get; init; } = string.Empty;
            }

            public sealed class Line
            {
                public int Quantity { get; init; }
            }

            public sealed class Money
            {
                public decimal Amount { get; init; }
            }

            public enum OrderStatus
            {
                Open,
                Shipped,
            }
        }
        """;

    /// <summary>
    /// The two names an example uses without introducing: where its output goes. Declared beside the example
    /// rather than in the fixture namespace, so a block written as statements can read them unqualified.
    /// </summary>
    private const string _locals = """
        static readonly string path = "out.ts";
        static readonly string outputDirectory = ".";
        """;

    /// <summary>The READMEs, each named so a failure says which file to open.</summary>
    public static TheoryData<string> Files =>
    [
        "README.md",
        Path.Combine("src", "SimpleTypeScript", "README.md"),
        Path.Combine("src", "SimpleTypeScript.TypeGeneration", "README.md"),
    ];

    [Theory]
    [MemberData(nameof(Files))]
    public void Every_csharp_example_compiles(string file)
    {
        var path = Path.Combine(Root(), file);
        Assert.True(File.Exists(path), $"{file} is listed here but not in the repository");

        var examples = Examples(File.ReadAllText(path));
        Assert.True(examples.Count > 0, $"{file} carries no C# example, so this fact holds nothing");

        for (var index = 0; index < examples.Count; index++)
        {
            var errors = Compile(examples[index]);
            Assert.True(
                errors.Length == 0,
                $"{file}, example {index + 1} does not compile:\n{errors}\n\n{examples[index]}");
        }
    }

    /// <summary>
    /// The fenced <c>csharp</c> blocks, in order. A <c>ts</c> block beside one is what the example produces
    /// and is checked by reading, not by compiling — there is no TypeScript compiler in this process.
    /// </summary>
    private static List<string> Examples(string markdown)
    {
        var examples = new List<string>();
        var builder = new StringBuilder();
        var inside = false;

        foreach (var line in markdown.Split('\n').Select(line => line.TrimEnd('\r')))
        {
            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                if (inside)
                {
                    examples.Add(builder.ToString());
                    builder.Clear();
                    inside = false;
                }
                else
                {
                    inside = line.Trim() is "```csharp" or "```cs";
                }

                continue;
            }

            if (inside)
            {
                builder.Append(line).Append('\n');
            }
        }

        return examples;
    }

    /// <summary>
    /// The example's compiler errors, or empty where it compiles. Tried as statements first and as
    /// declarations second, because a README shows both — a call sequence and the class a consumer writes —
    /// and which one a block is is not worth marking up in the markdown. Both attempts are reported when
    /// neither works, since the one that was meant is whichever error reads as a real mistake.
    /// </summary>
    private static string Compile(string example)
    {
        var statements = Errors(
            $"{_preamble}\ninternal static class Example\n{{\n{_locals}\nstatic void Run()\n{{\n{example}\n}}\n}}");
        if (statements.Length == 0)
        {
            return string.Empty;
        }

        var declarations = Errors($"{_preamble}\n{example}");

        return declarations.Length == 0
            ? string.Empty
            : $"as statements:\n{statements}\n\nas declarations:\n{declarations}";
    }

    private static string Errors(string source)
    {
        var compilation = CSharpCompilation.Create(
            "Examples",
            [CSharpSyntaxTree.ParseText(source)],
            References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var errors = compilation
            .GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            // An example is a fragment: it may declare something it does not use, and it is written to be
            // read rather than to be complete.
            .Where(diagnostic => diagnostic.Id is not ("CS0169" or "CS0414" or "CS8321"))
            .Select(diagnostic => diagnostic.ToString());

        return string.Join('\n', errors);
    }

    /// <summary>
    /// What this test project was compiled against: the packages and the framework beneath them.
    /// <para>
    /// The trusted-platform list rather than the loaded assemblies. An assembly is loaded when something has
    /// used it, and the example being compiled is exactly the thing that has not run yet — so a package a
    /// README documents and nothing else touches would be missing, and every line of that example would fail
    /// for a reason that is not about the example.
    /// </para>
    /// </summary>
    private static IReadOnlyList<MetadataReference> References() =>
    [
        .. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .Select(path => MetadataReference.CreateFromFile(path)),
    ];

    /// <summary>
    /// The repository root, found by walking up to the solution rather than by counting directories: the
    /// build output's depth changes with a configuration or a RID, and a count resolves somewhere wrong
    /// instead of failing.
    /// </summary>
    private static string Root()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SimpleTypeScript.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);

        return directory.FullName;
    }
}
