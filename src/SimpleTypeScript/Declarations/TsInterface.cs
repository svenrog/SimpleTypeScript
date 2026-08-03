using System.Text;

namespace SimpleTypeScript.Declarations;

/// <summary>
/// An exported <c>interface</c>: the shape of a value, where a <see cref="TsConst"/> is a value. Members are
/// written in the order given, because only the caller knows what a stable order is for what it generated
/// from.
/// </summary>
internal sealed class TsInterface : TsDeclaration
{
    private readonly IReadOnlyList<TsMember> _members;

    private TsInterface(string name, IReadOnlyList<TsMember> members, string? doc)
        : base(name, "interface", TsDeclarationSpace.Type, doc)
    {
        // An interface declaring nothing is assignable from anything, so a walk that reached no members
        // would generate a type that type-checks against every mistake it was supposed to catch.
        if (members.Count == 0)
        {
            throw new TypeScriptException($"interface {name} declares no members");
        }

        var declared = new HashSet<string>(members.Count, StringComparer.Ordinal);
        foreach (var member in members)
        {
            if (!declared.Add(member.Name))
            {
                throw new TypeScriptException($"interface {name} declares '{member.Name}' more than once");
            }
        }

        _members = members;
    }

    /// <summary>The interface, with <paramref name="members"/> in the order they are to be written.</summary>
    internal static TsInterface Create(string name, IEnumerable<TsMember> members, string? doc = null) =>
        new(name, [.. members], doc);

    /// <inheritdoc />
    private protected override void WriteBody(StringBuilder builder)
    {
        builder.Append("export interface ").Append(Name).Append(" {\n");
        foreach (var member in _members)
        {
            member.Write(builder, 1);
        }

        builder.Append('}');
    }
}
