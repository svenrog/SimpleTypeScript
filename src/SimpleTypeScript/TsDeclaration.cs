using System.Text;

namespace SimpleTypeScript;

/// <summary>
/// One exported declaration, held as what it is rather than as the text of it. A module that kept rendered
/// strings could not reorder, inspect or re-render what it holds, and its only real job would be joining —
/// so the parts stay parts until the whole module is written.
/// <para>
/// The doc comment belongs to the declaration for the same reason. Carried instead as pending state on the
/// module, it can be attached to the wrong one, or to nothing, by a caller that merely writes its statements
/// in an order that reads fine.
/// </para>
/// </summary>
internal sealed class TsDeclaration
{
    private readonly string _name;
    private readonly TsValue _value;
    private readonly TsType? _type;
    private readonly bool _asConst;
    private readonly TsComment? _doc;

    private TsDeclaration(string name, TsValue value, TsType? type, bool asConst, TsComment? doc)
    {
        _name = name;
        _value = value;
        _type = type;
        _asConst = asConst;
        _doc = doc;
    }

    /// <summary>
    /// An exported <c>const</c>. <paramref name="type"/> annotates it where the inferred type would be too
    /// narrow to assign into; <paramref name="asConst"/> keeps the literal's own type for a caller that
    /// indexes it by a known key. The two are mutually exclusive — <c>as const</c> on an annotated
    /// declaration is either redundant or a conflict, and TypeScript accepts some of those silently.
    /// </summary>
    internal static TsDeclaration Const(
        string name, TsValue value, TsType? type = null, bool asConst = false, string? doc = null)
    {
        if (!TsSyntax.IsIdentifier(name))
        {
            throw new TypeScriptException($"'{name}' is not a valid TypeScript binding name");
        }

        if (type is not null && asConst)
        {
            throw new TypeScriptException($"{name} is both annotated and `as const`; one of the two is meant");
        }

        return new TsDeclaration(name, value, type, asConst, doc is null ? null : TsComment.Doc(doc));
    }

    /// <summary>Appends the declaration, its doc comment first, ending in <c>;</c> and no newline.</summary>
    internal void Write(StringBuilder builder)
    {
        _doc?.Write(builder);

        builder.Append("export const ").Append(_name);
        if (_type is not null)
        {
            builder.Append(": ").Append(_type.Render());
        }

        builder.Append(" = ");
        _value.Write(builder, 0);
        builder.Append(_asConst ? " as const;" : ";");
    }
}
