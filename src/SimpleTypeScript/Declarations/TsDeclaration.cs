using SimpleTypeScript.Syntax;
using System.Text;

namespace SimpleTypeScript.Declarations;

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
internal abstract class TsDeclaration
{
    private readonly TsComment? _doc;

    private protected TsDeclaration(string name, string kind, TsDeclarationSpace space, string? doc)
    {
        if (!TsSyntax.IsIdentifier(name))
        {
            throw new TypeScriptException($"'{name}' is not a valid TypeScript {kind} name");
        }

        Name = name;
        Space = space;
        _doc = doc is null ? null : TsComment.Doc(doc);
    }

    /// <summary>What the declaration binds.</summary>
    internal string Name { get; }

    /// <summary>Where the name is bound, which is what decides whether a second one collides with it.</summary>
    internal TsDeclarationSpace Space { get; }

    /// <summary>Appends the declaration, its doc comment first, ending without a newline.</summary>
    internal void Write(StringBuilder builder)
    {
        _doc?.Write(builder);
        WriteBody(builder);
    }

    /// <summary>Appends everything after the doc comment.</summary>
    private protected abstract void WriteBody(StringBuilder builder);
}
