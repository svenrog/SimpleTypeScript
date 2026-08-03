namespace SimpleTypeScript;

/// <summary>
/// A TypeScript type, as an annotation on a declaration. Built from factories rather than written as a
/// string, so a type is spelled correctly once here instead of at each call site — <c>Record&lt;string,
/// string&gt;</c> written by hand is the kind of thing that becomes <c>Record&lt;string,string&gt;</c> in
/// half the modules.
/// <para>
/// <b>Deliberately not the whole type system.</b> Unions, intersections and conditionals have no shape in C#
/// to model them from, so carrying them would mean inventing one for a generator that emits records of
/// strings — and every level of a grammar that is modelled has to be kept correct. Every type here closes
/// with its own syntax, which is why nothing needs parenthesising and no precedence is tracked. A composed
/// type is refused by <see cref="Of"/> rather than half-supported.
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

    /// <summary>How the type is written.</summary>
    public abstract string Render();

    /// <summary>Whether <paramref name="name"/> is a bare type reference, optionally dotted for a namespace.</summary>
    private static bool IsTypeReference(string name) =>
        name.Length > 0 && name.Split('.').All(TsSyntax.IsIdentifier);
}
