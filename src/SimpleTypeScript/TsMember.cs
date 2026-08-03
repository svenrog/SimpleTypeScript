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
/// </summary>
public sealed class TsMember
{
    private readonly string _name;
    private readonly TsType _type;
    private readonly bool _isReadOnly;
    private readonly TsComment? _doc;

    /// <summary>
    /// A member of <paramref name="type"/>. <paramref name="isReadOnly"/> writes <c>readonly</c>, which is
    /// what a shape the consumer only ever receives should say.
    /// </summary>
    public TsMember(string name, TsType type, string? doc = null, bool isReadOnly = false)
    {
        if (name.Length == 0)
        {
            throw new TypeScriptException("a member has no name");
        }

        _name = name;
        _type = type;
        _isReadOnly = isReadOnly;
        _doc = doc is null ? null : TsComment.Doc(doc);
    }

    /// <summary>What the member is called, before any quoting.</summary>
    internal string Name => _name;

    /// <summary>Appends the member at <paramref name="depth"/>, ending in <c>;</c> and a newline.</summary>
    internal void Write(StringBuilder builder, int depth)
    {
        _doc?.Write(builder, depth);

        TsSyntax.AppendIndent(builder, depth);
        if (_isReadOnly)
        {
            builder.Append("readonly ");
        }

        if (TsSyntax.IsIdentifier(_name))
        {
            builder.Append(_name);
        }
        else
        {
            TsSyntax.AppendString(builder, _name);
        }

        builder.Append(": ").Append(_type.Render()).Append(";\n");
    }
}
