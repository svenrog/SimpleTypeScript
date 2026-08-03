using Xunit;

namespace SimpleTypeScript.Tests;

/// <summary>
/// The declarations that describe a shape rather than a value: <c>interface</c>, <c>type</c>, and the union a
/// closed set becomes on a JSON wire. Same standard as the value tests — the output is compiled by
/// <c>tsc</c>, never read first, so a rule that is only probably right fails where nobody is looking.
/// </summary>
public sealed class TypeScriptShapeTests
{
    private const char _lineSeparator = (char)0x2028;

    private static string Render(Action<TsModule> declare)
    {
        var module = new TsModule();
        declare(module);

        return module.Render(TsComment.Empty());
    }

    [Fact]
    public void Writes_an_interface_a_member_to_a_line()
    {
        var rendered = Render(module => module.Interface(
            "ScanSummary",
            [
                new TsMember("id", TsType.String),
                new TsMember("findings", TsType.ArrayOf(TsType.Of("Finding"))),
            ]));

        Assert.Contains(
            "export interface ScanSummary {\n  readonly id: string;\n  readonly findings: Finding[];\n}",
            rendered,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// An interface is read as code beside the hand-written types around it, so a name that needs no quoting
    /// does not get any — unlike an object literal's keys, which are scanned as data and quoted throughout.
    /// </summary>
    [Fact]
    public void Quotes_a_member_name_only_where_it_could_not_be_written_bare()
    {
        var rendered = Render(module => module.Interface(
            "Headers",
            [
                new TsMember("contentType", TsType.String),
                new TsMember("content-type", TsType.String),
                new TsMember("class", TsType.String),
            ]));

        Assert.Contains("  readonly contentType: string;", rendered, StringComparison.Ordinal);
        Assert.Contains("  readonly \"content-type\": string;", rendered, StringComparison.Ordinal);

        // A reserved word is a legal property name in modern ECMAScript, and IsIdentifier is conservative
        // about it. Over-quoting is the harmless direction; under-quoting is a syntax error.
        Assert.Contains("  readonly \"class\": string;", rendered, StringComparison.Ordinal);
    }

    /// <summary>
    /// <c>readonly</c> is what a shape the consumer only receives says, so it is what a member says unless
    /// told otherwise — and a payload the consumer also builds turns it off per member.
    /// </summary>
    [Fact]
    public void Writes_readonly_and_a_doc_comment_on_the_member_they_belong_to()
    {
        var rendered = Render(module => module.Interface(
            "Scan",
            [
                new TsMember("id", TsType.String) { Doc = "Assigned when the scan is queued." },
                new TsMember("domain", TsType.String) { IsReadOnly = false },
            ],
            doc: "One scan, as the API returns it."));

        Assert.Contains(
            "/** One scan, as the API returns it. */\nexport interface Scan {\n"
            + "  /** Assigned when the scan is queued. */\n  readonly id: string;\n  domain: string;\n}",
            rendered,
            StringComparison.Ordinal);
    }

    /// <summary><c>null</c> is a reserved word, so the type has to be handed out rather than spelled.</summary>
    [Fact]
    public void Names_the_null_type_that_a_reference_could_not()
    {
        Assert.Equal("string | null", TsType.Union([TsType.String, TsType.Null]).Render());
        Assert.Throws<TypeScriptException>(() => TsType.Of("null"));
    }

    [Fact]
    public void Writes_a_union_alias_in_the_order_given()
    {
        var rendered = Render(module => module.TypeAlias(
            "ScanStatus",
            TsType.Union([TsType.StringLiteral("Queued"), TsType.StringLiteral("Running")])));

        Assert.Contains("export type ScanStatus = \"Queued\" | \"Running\";", rendered, StringComparison.Ordinal);
    }

    /// <summary>
    /// The one place this model tracks precedence. <c>"a" | "b"[]</c> is a union with an array in it — a
    /// different type that compiles just as well, which is what makes the missing parentheses invisible.
    /// </summary>
    [Fact]
    public void Parenthesises_a_union_inside_an_array_and_nothing_else()
    {
        var union = TsType.Union([TsType.StringLiteral("a"), TsType.StringLiteral("b")]);

        Assert.Equal("(\"a\" | \"b\")[]", TsType.ArrayOf(union).Render());
        Assert.Equal("\"a\" | \"b\"", union.Render());
        Assert.Equal("string[]", TsType.ArrayOf(TsType.String).Render());
        Assert.Equal("Record<string, \"a\" | \"b\">", TsType.Record(TsType.String, union).Render());
    }

    /// <summary>A literal type is a string literal, so it carries the same escaping and the same reason.</summary>
    [Fact]
    public void Escapes_a_union_member_for_source_the_way_any_other_string_is()
    {
        var rendered = Render(module => module.TypeAlias(
            "Odd",
            TsType.Union([TsType.StringLiteral($"a{_lineSeparator}b"), TsType.StringLiteral("say \"hi\"")])));

        Assert.Contains("\"a\\u2028b\" | \"say \\\"hi\\\"\"", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain(_lineSeparator, rendered);
    }

    /// <summary>A set of one is a legitimate thing to generate; writing it as a union would add punctuation.</summary>
    [Fact]
    public void Collapses_a_union_of_one_and_refuses_a_union_of_none()
    {
        Assert.Equal("\"only\"", TsType.Union([TsType.StringLiteral("only")]).Render());
        Assert.Equal("only[]", TsType.ArrayOf(TsType.Union([TsType.Of("only")])).Render());
        Assert.Throws<TypeScriptException>(() => TsType.Union([]));
    }

    /// <summary>
    /// An interface declaring nothing is assignable from anything, so it type-checks against every mistake it
    /// was generated to catch — and a member written twice silently loses one of the two.
    /// </summary>
    [Fact]
    public void Refuses_a_shape_that_would_check_nothing()
    {
        Assert.Throws<TypeScriptException>(() => new TsModule().Interface("Empty", []));
        Assert.Throws<TypeScriptException>(() => new TsModule().Interface(
            "Twice", [new TsMember("id", TsType.String), new TsMember("id", TsType.Number)]));
        Assert.Throws<TypeScriptException>(() => new TsModule().Interface(
            "not a name", [new TsMember("id", TsType.String)]));
        Assert.Throws<TypeScriptException>(() => new TsModule().TypeAlias("class", TsType.String));
        Assert.Throws<TypeScriptException>(() => new TsMember("", TsType.String));
    }

    /// <summary>
    /// The same fact the value side holds: the same declarations produce the same bytes wherever the
    /// generator runs, so regenerating on another machine does not rewrite a file that did not change.
    /// </summary>
    [Fact]
    public void Writes_only_line_feeds_and_separates_declarations_by_one_blank_line()
    {
        var rendered = Render(module => module
            .TypeAlias("Status", TsType.Union([TsType.StringLiteral("a"), TsType.StringLiteral("b")]))
            .Interface("Scan", [new TsMember("status", TsType.Of("Status"))]));

        Assert.DoesNotContain('\r', rendered);
        Assert.Contains(
            "export type Status = \"a\" | \"b\";\n\nexport interface Scan {\n  readonly status: Status;\n}\n",
            rendered,
            StringComparison.Ordinal);
    }
}
