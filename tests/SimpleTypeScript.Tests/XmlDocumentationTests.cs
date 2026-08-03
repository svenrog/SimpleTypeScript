using SimpleTypeScript.TypeGeneration.Documentation;
using Xunit;

namespace SimpleTypeScript.Tests;

/// <summary>
/// What a doc comment becomes on the other side of the wire. Doc XML is markup for a renderer TypeScript
/// does not have, so what reaches a generated comment is a flattening — and a flattening has rules, which is
/// what a reader of the generated file is actually relying on.
/// </summary>
public sealed class XmlDocumentationTests
{
    /// <summary>
    /// Authored rather than read off this assembly's own file: the rules worth pinning are the ones a real
    /// summary only sometimes exercises, and a fixture that has to contain all of them is one nobody can edit.
    /// </summary>
    private const string _document = """
        <?xml version="1.0"?>
        <doc>
          <members>
            <member name="T:SimpleTypeScript.Tests.XmlDocumentationTests">
              <summary>
              Reads a <see cref="T:System.Text.StringBuilder"/>, a <c>nested</c> element
              and <see langword="null"/>, across
              lines.
              </summary>
              <remarks>Which is not a summary.</remarks>
            </member>
            <member name="T:SimpleTypeScript.Tests.XmlDocumentationTests.Shape">
              <summary>A shape declared inside another.</summary>
            </member>
            <member name="P:SimpleTypeScript.Tests.XmlDocumentationTests.Shape.Count">
              <summary>How many.</summary>
            </member>
          </members>
        </doc>
        """;

    /// <summary>
    /// A cref is a fully qualified C# name and means nothing to a consumer, an element resolves to its own
    /// text, and the whitespace that separated them in the source is not a paragraph break. The punctuation
    /// closing a resolved element has to stay against it: a gap in front of a comma reads as a mistake.
    /// </summary>
    [Fact]
    public void Flattens_a_summary_into_one_sentence()
    {
        var directory = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var assembly = typeof(XmlDocumentationTests).Assembly.GetName().Name;
            File.WriteAllText(Path.Combine(directory, $"{assembly}.xml"), _document);

            var summary = new XmlDocumentationSource(directory).For(typeof(XmlDocumentationTests));

            Assert.Equal("Reads a StringBuilder, a nested element and null, across lines.", summary);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// A DTO declared inside the type that owns it is ordinary, and its documentation is filed under a name
    /// the runtime does not use: nesting is a <c>+</c> in <c>Type.FullName</c> and a <c>.</c> in the
    /// compiler's own file, so asking with the first finds nothing and says nothing about why.
    /// </summary>
    [Fact]
    public void Finds_the_comment_on_a_type_declared_inside_another()
    {
        var directory = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var assembly = typeof(XmlDocumentationTests).Assembly.GetName().Name;
            File.WriteAllText(Path.Combine(directory, $"{assembly}.xml"), _document);

            var source = new XmlDocumentationSource(directory);

            Assert.Equal("A shape declared inside another.", source.For(typeof(Shape)));
            Assert.Equal("How many.", source.For(typeof(Shape).GetProperty(nameof(Shape.Count))!));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// An assembly that ships no XmlDoc is a member without a comment rather than a broken build: the file is
    /// the compiler's to write, and a consumer generating from a package has no say in whether it was.
    /// </summary>
    [Fact]
    public void Reads_an_assembly_with_no_documentation_as_no_comments()
    {
        var directory = Directory.CreateTempSubdirectory().FullName;
        try
        {
            Assert.Null(new XmlDocumentationSource(directory).For(typeof(XmlDocumentationTests)));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>A shape declared inside another, which is where a DTO usually lives.</summary>
    private sealed class Shape
    {
        public int Count { get; init; }
    }
}
