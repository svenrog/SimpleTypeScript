using System.Text;

namespace SimpleTypeScript.Declarations;

/// <summary>An exported <c>const</c> bound to a value literal.</summary>
internal sealed class TsConst : TsDeclaration
{
    private readonly TsValue _value;
    private readonly TsType? _type;
    private readonly bool _asConst;

    private TsConst(string name, TsValue value, TsType? type, bool asConst, string? doc)
        : base(name, "binding", doc)
    {
        _value = value;
        _type = type;
        _asConst = asConst;
    }

    /// <summary>
    /// <paramref name="type"/> annotates the declaration where the inferred type would be too narrow to
    /// assign into; <paramref name="asConst"/> keeps the literal's own type for a caller that indexes it by a
    /// known key. The two are mutually exclusive — <c>as const</c> on an annotated declaration is either
    /// redundant or a conflict, and TypeScript accepts some of those silently.
    /// </summary>
    internal static TsConst Create(
        string name, TsValue value, TsType? type = null, bool asConst = false, string? doc = null)
    {
        if (type is not null && asConst)
        {
            throw new TypeScriptException($"{name} is both annotated and `as const`; one of the two is meant");
        }

        return new TsConst(name, value, type, asConst, doc);
    }

    /// <inheritdoc />
    private protected override void WriteBody(StringBuilder builder)
    {
        builder.Append("export const ").Append(Name);
        if (_type is not null)
        {
            builder.Append(": ").Append(_type.Render());
        }

        builder.Append(" = ");
        _value.Write(builder, 0);
        builder.Append(_asConst ? " as const;" : ";");
    }
}
