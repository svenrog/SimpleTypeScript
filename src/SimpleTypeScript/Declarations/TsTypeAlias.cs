using System.Text;

namespace SimpleTypeScript.Declarations;

/// <summary>
/// An exported <c>type</c>: a name for a type that has no declaration of its own. What a set of string
/// literals is called, most of the time.
/// </summary>
internal sealed class TsTypeAlias : TsDeclaration
{
    private readonly TsType _type;

    private TsTypeAlias(string name, TsType type, string? doc)
        : base(name, "type", doc)
    {
        _type = type;
    }

    /// <summary>The alias binding <paramref name="name"/> to <paramref name="type"/>.</summary>
    internal static TsTypeAlias Create(string name, TsType type, string? doc = null) => new(name, type, doc);

    /// <inheritdoc />
    private protected override void WriteBody(StringBuilder builder) =>
        builder.Append("export type ").Append(Name).Append(" = ").Append(_type.Render()).Append(';');
}
