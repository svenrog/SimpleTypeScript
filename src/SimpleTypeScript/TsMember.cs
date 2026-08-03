using SimpleTypeScript.Syntax;
using System.Text;

namespace SimpleTypeScript;

/// <summary>
/// One property signature in an interface.
/// <para>
/// The name is written bare where it is an identifier and quoted where it is not, rather than quoted
/// throughout the way an object literal's keys are. The two are read differently: a generated map is scanned
/// as data, where uniform quoting helps, while an interface is read as code beside the hand-written types
/// around it.
/// </para>
/// <para>
/// <b>What the member is, it is constructed with; how it is written is set on it.</b> A name and a type are
/// the member, and both are checked; the rest is how the same member appears, and each of those read as a
/// bare <c>true</c> at a call site that says which one it is nowhere. It is also what keeps a modifier this
/// does not carry yet from moving every caller when it arrives.
/// </para>
/// </summary>
public sealed class TsMember
{
    /// <summary>A member called <paramref name="name"/>, holding <paramref name="type"/>.</summary>
    public TsMember(string name, TsType type)
    {
        if (name.Length == 0)
        {
            throw new TypeScriptException("a member has no name");
        }

        Name = name;
        Type = type;
    }

    /// <summary>What the member is called, before any quoting.</summary>
    public string Name { get; }

    /// <summary>What it holds.</summary>
    public TsType Type { get; }

    /// <summary>
    /// Writes <c>readonly</c>, which is what a shape the consumer only ever receives should say. Off by
    /// default: the emitter writes what it is told, and the opinion about what a received payload looks like
    /// belongs to the layer that reads the C#.
    /// </summary>
    public bool IsReadOnly { get; init; }

    /// <summary>
    /// Writes <c>?</c>, for a member the producer omits rather than sends empty — which is a different thing
    /// from one it sends as <c>null</c>.
    /// </summary>
    public bool IsOptional { get; init; }

    /// <summary>The comment an editor surfaces at the use site, or <c>null</c> for none.</summary>
    public string? Doc { get; init; }

    /// <summary>Appends the member at <paramref name="depth"/>, ending in <c>;</c> and a newline.</summary>
    internal void Write(StringBuilder builder, int depth)
    {
        if (Doc is not null)
        {
            TsComment.Doc(Doc).Write(builder, depth);
        }

        TsSyntax.AppendIndent(builder, depth);
        if (IsReadOnly)
        {
            builder.Append("readonly ");
        }

        if (TsSyntax.IsIdentifier(Name))
        {
            builder.Append(Name);
        }
        else
        {
            TsSyntax.AppendString(builder, Name);
        }

        if (IsOptional)
        {
            builder.Append('?');
        }

        builder.Append(": ");
        Type.Write(builder);
        builder.Append(";\n");
    }
}
