using SimpleTypeScript.Syntax;
using SimpleTypeScript.Types;

namespace SimpleTypeScript;

/// <summary>
/// A TypeScript type, as an annotation on a declaration. Built from factories rather than written as a
/// string, so a type is spelled correctly once here instead of at each call site — <c>Record&lt;string,
/// string&gt;</c> written by hand is the kind of thing that becomes <c>Record&lt;string,string&gt;</c> in
/// half the modules.
/// <para>
/// <b>Deliberately not the whole type system.</b> A union is here because a closed set is what a JSON wire
/// carries an enumeration as, and there is nothing else to spell it with; intersections, conditionals and
/// generics beyond the two named below are not, because every level of a grammar that is modelled has to be
/// kept correct. A composed type written as a <em>name</em> stays refused by <see cref="Of"/> — a union is
/// something to build, not something to spell.
/// </para>
/// </summary>
public abstract class TsType
{
    private protected TsType()
    {
    }

    /// <summary>The <c>string</c> primitive.</summary>
    public static TsType String { get; } = new TsNamedType("string");

    /// <summary>The <c>number</c> primitive.</summary>
    public static TsType Number { get; } = new TsNamedType("number");

    /// <summary>The <c>boolean</c> primitive.</summary>
    public static TsType Boolean { get; } = new TsNamedType("boolean");

    /// <summary>
    /// The <c>null</c> type, which a union names to say a value may be absent. A keyword rather than a
    /// reference, so <see cref="Of"/> cannot produce it — that refuses reserved words, and rightly.
    /// </summary>
    public static TsType Null { get; } = new TsNamedType("null");

    /// <summary>
    /// A type referred to by name — an interface the module imports, or a primitive this model does not
    /// carry. The name must be a plain reference, optionally dotted: anything composed is a type this model
    /// does not describe, and accepting it as text would let a caller write one it cannot render correctly.
    /// </summary>
    public static TsType Of(string name) =>
        IsTypeReference(name)
            ? new TsNamedType(name)
            : throw new TypeScriptException($"'{name}' is not a plain type reference");

    /// <summary><c>Record&lt;key, value&gt;</c>.</summary>
    public static TsType Record(TsType key, TsType value) => new TsGenericType("Record", [key, value]);

    /// <summary><c>item[]</c>.</summary>
    public static TsType ArrayOf(TsType item) => new TsArrayType(item);

    /// <summary>One string, as a type — what a member of a closed set is, once it is on a JSON wire.</summary>
    public static TsType StringLiteral(string value) => new TsStringLiteralType(value);

    /// <summary>
    /// <c>a | b | c</c>, in the order given. A union of one is that type: a set with a single member is a
    /// legitimate thing to generate, and writing it as a union would only add punctuation.
    /// </summary>
    public static TsType Union(IEnumerable<TsType> members)
    {
        var alternatives = members.ToArray();

        return alternatives.Length switch
        {
            0 => throw new TypeScriptException("a union has no alternatives"),
            1 => alternatives[0],
            _ => new TsUnionType(alternatives),
        };
    }

    /// <summary>How the type is written.</summary>
    public abstract string Render();

    /// <summary>
    /// Whether a container has to parenthesise this type. The whole of the precedence model, and deliberately
    /// a flag rather than a rank: only a union needs it, and a second construct that did would be the point
    /// at which guessing stops being safe.
    /// </summary>
    internal virtual bool RequiresParentheses => false;

    /// <summary>Whether <paramref name="name"/> is a bare type reference, optionally dotted for a namespace.</summary>
    private static bool IsTypeReference(string name) =>
        name.Length > 0 && name.Split('.').All(TsSyntax.IsIdentifier);
}
