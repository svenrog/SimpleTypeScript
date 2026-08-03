using SimpleTypeScript.Syntax;
using SimpleTypeScript.Values;
using System.Text;

namespace SimpleTypeScript;

/// <summary>
/// A TypeScript value literal. The caller builds one explicitly instead of handing an object to a
/// serializer, so nothing reflects over a type and what is emitted is what the generator said to emit — and
/// the emitter carries no trimming or AOT caveat into a consumer that publishes one.
/// <para>
/// The constructor is <c>private protected</c>: a value may only be added from inside this assembly, so the
/// one path text takes to a file still runs through <see cref="TsSyntax"/> rather than around it.
/// </para>
/// <para>
/// Each value writes itself at a given depth, so nesting composes without any level knowing the shape above
/// it.
/// </para>
/// </summary>
public abstract class TsValue
{
    private protected TsValue()
    {
    }

    /// <summary><c>null</c>.</summary>
    public static TsValue Null { get; } = new TsRawValue("null");

    /// <summary>A string literal, escaped for ECMAScript source.</summary>
    public static TsValue String(string value) => new TsStringValue(value);

    /// <summary>A number literal, invariant and round-trippable.</summary>
    public static TsValue Number(double value) => new TsRawValue(TsSyntax.Number(value));

    /// <summary><c>true</c> or <c>false</c>.</summary>
    public static TsValue Boolean(bool value) => new TsRawValue(value ? "true" : "false");

    /// <summary>An array literal.</summary>
    public static TsValue Array(IEnumerable<TsValue> items) => new TsArrayValue([.. items]);

    /// <summary>An array of strings — the common case, spelled once.</summary>
    public static TsValue Array(IEnumerable<string> items) => Array(items.Select(String));

    /// <summary>
    /// An object literal, in the order given. Order is the caller's: a generated module should be byte-stable
    /// across runs, and only the caller knows what stable means for its data.
    /// </summary>
    public static TsValue Object(IEnumerable<KeyValuePair<string, TsValue>> entries) =>
        new TsObjectValue([.. entries]);

    /// <summary>An object literal whose values are all strings — the common case, spelled once.</summary>
    public static TsValue Object(IEnumerable<KeyValuePair<string, string>> entries) =>
        Object(entries.Select(entry => KeyValuePair.Create(entry.Key, String(entry.Value))));

    /// <summary>
    /// Writes this value at <paramref name="depth"/>. The opening token goes where the caller already is — a
    /// value never indents its own first line, because it may follow <c>= </c> on a line already begun.
    /// </summary>
    internal abstract void Write(StringBuilder builder, int depth);

    /// <summary>
    /// Whether this value fits on the line it starts, so a container can keep itself compact. Assembly-wide
    /// rather than <c>private protected</c>: a container asks it of its <em>members</em>, which it holds as
    /// <see cref="TsValue"/>, and a protected member cannot be read through the base type.
    /// </summary>
    internal abstract bool IsInline { get; }

    /// <summary>Indents to <paramref name="depth"/>, for a container writing a member on its own line.</summary>
    private protected static void Indent(StringBuilder builder, int depth) =>
        TsSyntax.AppendIndent(builder, depth);
}
