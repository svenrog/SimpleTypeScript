using SimpleTypeScript.Values;
using System.Text;

namespace SimpleTypeScript.Declarations;

/// <summary>An exported <c>const</c> bound to a value literal.</summary>
internal sealed class TsConst : TsDeclaration
{
    private readonly TsValue _value;
    private readonly TsType? _type;

    private TsConst(string name, TsValue value, TsType? type, string? doc)
        : base(name, "binding", TsDeclarationSpace.Value, doc)
    {
        _value = value;
        _type = type;
    }

    /// <summary>
    /// <paramref name="type"/> annotates the declaration where the inferred type would be too narrow to
    /// assign into. It cannot be given for a value asserted <c>as const</c>: that assertion makes the
    /// literal readonly, and an annotation is then either redundant or the conflict of assigning a readonly
    /// literal into a mutable shape — which TypeScript accepts some of silently.
    /// </summary>
    internal static TsConst Create(string name, TsValue value, TsType? type = null, string? doc = null)
    {
        if (type is not null && value is TsAsConstValue)
        {
            throw new TypeScriptException($"{name} is both annotated and `as const`; one of the two is meant");
        }

        return new TsConst(name, value, type, doc);
    }

    /// <inheritdoc />
    private protected override void WriteBody(StringBuilder builder)
    {
        builder.Append("export const ").Append(Name);
        if (_type is not null)
        {
            builder.Append(": ");
            _type.Write(builder);
        }

        builder.Append(" = ");
        _value.Write(builder, 0);
        builder.Append(';');
    }
}
