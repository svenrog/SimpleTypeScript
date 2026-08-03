using Xunit;

namespace SimpleTypeScript.Tests;

/// <summary>
/// What the emitter writes. Generated TypeScript is compiled by <c>tsc</c> and never read before it runs, so
/// a rule that is merely probably right produces a file that is broken in a way no reviewer sees — which is
/// why the escaping and quoting rules are asserted rather than reasoned about.
/// </summary>
public sealed class TypeScriptEmitterTests
{
    private const char _lineSeparator = (char)0x2028;
    private const char _paragraphSeparator = (char)0x2029;

    private static string Render(TsValue value) =>
        new TsModule().Const("X", value).Render(TsComment.Empty());

    // Text is escaped for ECMAScript source, not for JSON: everything a string literal cannot carry raw.
    [Theory]
    [InlineData("plain", "\"plain\"")]
    [InlineData("say \"hi\"", "\"say \\\"hi\\\"\"")]
    [InlineData("back\\slash", "\"back\\\\slash\"")]
    [InlineData("line\nbreak", "\"line\\nbreak\"")]
    [InlineData("tab\there", "\"tab\\there\"")]
    [InlineData("del\u007fete", "\"del\\u007fete\"")]
    [InlineData("next\u0085line", "\"next\\u0085line\"")]
    public void Escapes_what_a_string_literal_cannot_carry(string value, string expected) =>
        Assert.Contains(expected, Render(TsValue.String(value)), StringComparison.Ordinal);

    /// <summary>
    /// The two characters JSON is happy to leave raw and ECMAScript ends a line on. A serializer that treats
    /// this as JSON emits them literally, and the module stops parsing where the string was supposed to be.
    /// </summary>
    [Fact]
    public void Escapes_the_separators_that_end_a_line_in_source_but_not_in_json()
    {
        var rendered = Render(TsValue.String($"a{_lineSeparator}b{_paragraphSeparator}c"));

        Assert.Contains("\"a\\u2028b\\u2029c\"", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain(_lineSeparator, rendered);
        Assert.DoesNotContain(_paragraphSeparator, rendered);
    }

    /// <summary>Readability is the point of not escaping everything above ASCII — so this must stay literal.</summary>
    [Fact]
    public void Leaves_text_a_reader_checks_a_translation_in_alone()
    {
        Assert.Contains("\"köad för omanalys\"", Render(TsValue.String("köad för omanalys")), StringComparison.Ordinal);
    }

    /// <summary>A half of a surrogate pair cannot be encoded as well-formed UTF-8, so it is escaped instead.</summary>
    [Fact]
    public void Escapes_a_lone_surrogate_but_not_a_paired_one()
    {
        Assert.Contains("\\ud83d\"", Render(TsValue.String("\ud83d")), StringComparison.Ordinal);
        Assert.Contains("\"\ud83d\ude80\"", Render(TsValue.String("\ud83d\ude80")), StringComparison.Ordinal);
    }

    /// <summary>
    /// Pairing is the one question that cannot be answered about a character on its own, so a string holding
    /// a surrogate is escaped by a different route than one that is not. The two have to agree about
    /// everything else in it.
    /// </summary>
    [Fact]
    public void Escapes_the_same_beside_an_astral_character()
    {
        Assert.Contains(
            "\"say \\\"hi\\\"\\n\ud83d\ude80\"",
            Render(TsValue.String("say \"hi\"\n\ud83d\ude80")),
            StringComparison.Ordinal);
    }

    [Fact]
    public void Writes_an_object_across_lines_with_a_trailing_comma()
    {
        var rendered = Render(TsValue.Object(new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" }));

        Assert.Contains("{\n  \"a\": \"1\",\n  \"b\": \"2\",\n}", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void Writes_a_primitive_array_on_one_line_and_an_empty_one_closed()
    {
        Assert.Contains("[\"a\",\"b\"]", Render(TsValue.Array(["a", "b"])), StringComparison.Ordinal);
        Assert.Contains("= [];", Render(TsValue.Array(Enumerable.Empty<string>())), StringComparison.Ordinal);
        Assert.Contains("= {};", Render(TsValue.Object(new Dictionary<string, string>())), StringComparison.Ordinal);
    }

    [Fact]
    public void Indents_a_nested_object_relative_to_its_parent()
    {
        var inner = TsValue.Object(new Dictionary<string, string> { ["k"] = "v" });
        var rendered = Render(TsValue.Object([KeyValuePair.Create("outer", inner)]));

        Assert.Contains("  \"outer\": {\n    \"k\": \"v\",\n  },\n", rendered, StringComparison.Ordinal);
    }

    // Every type closes with its own syntax, so an element never needs parenthesising — which is why no
    // precedence is modelled. A composed type is refused rather than rendered wrongly.
    [Fact]
    public void Composes_the_types_a_generated_module_actually_carries()
    {
        Assert.Equal("string[]", TsType.ArrayOf(TsType.String).Render());
        Assert.Equal("Record<string, string>", TsType.Record(TsType.String, TsType.String).Render());
        Assert.Equal("Record<string, Record<string, string>>[]",
            TsType.ArrayOf(TsType.Record(TsType.String, TsType.Record(TsType.String, TsType.String))).Render());
    }

    [Fact]
    public void Refuses_a_composed_type_written_as_a_name()
    {
        Assert.Throws<TypeScriptException>(() => TsType.Of("A | B"));
        Assert.Equal("Acme.Thing", TsType.Of("Acme.Thing").Render());
    }

    /// <summary>
    /// A banner and the code under it are two things, and one blank line is what says so. Asserted because
    /// nothing else would notice: a module missing it still compiles, still type-checks, and reads as a file
    /// whose first declaration is part of the notice above it.
    /// </summary>
    [Fact]
    public void Separates_a_header_from_the_first_declaration_by_one_blank_line()
    {
        Assert.StartsWith(
            "// note\n\nexport const X",
            new TsModule().Const("X", TsValue.Null).Render(TsComment.Lines(["note"])),
            StringComparison.Ordinal);

        // The default banner is a header like any other, and gets the same gap.
        Assert.Contains(
            "— do not edit.\n\nexport const X",
            new TsModule().Const("X", TsValue.Null).Render(),
            StringComparison.Ordinal);

        // No banner, no gap: the file starts at its first declaration.
        Assert.StartsWith(
            "export const X",
            new TsModule().Const("X", TsValue.Null).Render(TsComment.Empty()),
            StringComparison.Ordinal);
    }

    /// <summary>
    /// A comment ends at a line terminator, so text carrying one would put the rest of it into the module as
    /// code. Every terminator has to re-open the comment, including the two only ECMAScript treats as one.
    /// </summary>
    [Fact]
    public void Keeps_a_multi_line_header_inside_the_comment()
    {
        var rendered = new TsModule()
            .Const("X", TsValue.String("v"))
            .Render(TsComment.Lines([$"first\nsecond{_lineSeparator}third"]));

        Assert.StartsWith("// first\n// second\n// third\n", rendered, StringComparison.Ordinal);
    }

    /// <summary>Only a block comment can be closed from inside, so only there is the sequence broken.</summary>
    [Fact]
    public void Breaks_a_comment_terminator_in_a_doc_but_leaves_a_line_comment_alone()
    {
        var doc = new TsModule().Const("X", TsValue.String("v"), doc: "ends */ here").Render(TsComment.Empty());
        Assert.Contains("/** ends *\\/ here */", doc, StringComparison.Ordinal);

        var header = new TsModule().Const("X", TsValue.String("v")).Render(TsComment.Lines(["a */ b"]));
        Assert.Contains("// a */ b", header, StringComparison.Ordinal);
    }

    /// <summary>
    /// A module declaring one name twice compiles as whichever declaration the tooling resolved, so the
    /// shape a consumer is checked against is the one nobody chose. TypeScript's two namespaces are
    /// separate, though — a <c>const</c> and a type of one name is the pair an enum deliberately writes.
    /// </summary>
    [Fact]
    public void Refuses_one_name_declared_twice_in_the_space_that_holds_it()
    {
        Assert.Throws<TypeScriptException>(() => new TsModule()
            .TypeAlias("Status", TsType.String)
            .Interface("Status", [new TsMember("id", TsType.String)]));

        Assert.Throws<TypeScriptException>(() => new TsModule()
            .Const("X", TsValue.Null)
            .Const("X", TsValue.Null));

        var pair = new TsModule()
            .Const("Status", TsValue.Object(new Dictionary<string, string> { ["Open"] = "Open" }), asConst: true)
            .TypeAlias("Status", TsType.ValuesOf("Status"))
            .Render(TsComment.Empty());

        Assert.Contains("export const Status = {", pair, StringComparison.Ordinal);
        Assert.Contains("export type Status = typeof Status[keyof typeof Status];", pair, StringComparison.Ordinal);
    }

    /// <summary>
    /// A member the producer omits is a different thing from one it sends as <c>null</c>: the first is
    /// absent from the payload, and a consumer that reads it without checking gets <c>undefined</c>.
    /// </summary>
    [Fact]
    public void Writes_an_optional_member_as_one()
    {
        var rendered = new TsModule()
            .Interface("Shape", [new TsMember("note", TsType.String, isReadOnly: true, isOptional: true)])
            .Render(TsComment.Empty());

        Assert.Contains("  readonly note?: string;\n", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void Refuses_a_declaration_that_could_not_be_bound_or_read()
    {
        Assert.Throws<TypeScriptException>(() => new TsModule().Const("not a name", TsValue.Null));
        Assert.Throws<TypeScriptException>(() => new TsModule().Const("class", TsValue.Null));
        Assert.Throws<TypeScriptException>(
            () => new TsModule().Const("X", TsValue.Null, TsType.String, asConst: true));
        Assert.Throws<TypeScriptException>(() => new TsModule().Render(TsComment.Empty()));
    }

    /// <summary>
    /// The same declarations must produce the same bytes wherever the generator runs, or regenerating on
    /// another machine rewrites every line of a file that did not change.
    /// </summary>
    [Fact]
    public void Writes_only_line_feeds_whatever_the_platform_spells_a_newline_as()
    {
        var rendered = new TsModule()
            .Const("X", TsValue.Object(new Dictionary<string, string> { ["a"] = "1" }), doc: "A doc.")
            .Render(TsComment.Lines(["header"]));

        Assert.DoesNotContain('\r', rendered);
        Assert.EndsWith("\n", rendered, StringComparison.Ordinal);
    }

    /// <summary>Numbers are invariant, and the three values with no literal form are refused rather than guessed at.</summary>
    [Fact]
    public void Writes_numbers_a_parser_reads_the_same_in_every_culture()
    {
        Assert.Contains("= 1.5;", Render(TsValue.Number(1.5)), StringComparison.Ordinal);
        Assert.Throws<TypeScriptException>(() => TsValue.Number(double.NaN));
        Assert.Throws<TypeScriptException>(() => TsValue.Number(double.PositiveInfinity));
    }
}
