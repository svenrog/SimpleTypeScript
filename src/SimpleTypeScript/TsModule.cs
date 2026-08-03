using SimpleTypeScript.Declarations;
using System.Text;

namespace SimpleTypeScript;

/// <summary>
/// A TypeScript module: a header comment, then exported declarations. It holds declarations rather than
/// their text, so what a module <em>is</em> stays inspectable until the moment it is written — and the
/// spelling of a declaration is decided in one place, which is what lets a second generator inherit the
/// formatting instead of restating it and drifting from the first.
/// </summary>
public sealed class TsModule
{
    private readonly List<TsDeclaration> _declarations = [];
    private readonly HashSet<(TsDeclarationSpace Space, string Name)> _bound = [];

    /// <summary>Adds an exported <c>const</c>. See <see cref="TsConst.Create"/> for the arguments.</summary>
    public TsModule Const(
        string name, TsValue value, TsType? type = null, bool asConst = false, string? doc = null) =>
        Add(TsConst.Create(name, value, type, asConst, doc));

    /// <summary>Adds an exported <c>interface</c>, its members in the order given.</summary>
    public TsModule Interface(string name, IEnumerable<TsMember> members, string? doc = null) =>
        Add(TsInterface.Create(name, members, doc));

    /// <summary>Adds an exported <c>type</c> alias.</summary>
    public TsModule TypeAlias(string name, TsType type, string? doc = null) =>
        Add(TsTypeAlias.Create(name, type, doc));

    /// <summary>
    /// Adds the declaration, refusing a name already bound in the same space. A module declaring one name
    /// twice compiles as whichever the tooling resolved, so the shape a consumer checks against is the one
    /// nobody chose. The two spaces are separate — a <c>const</c> and a type of one name is a pair a
    /// generated enum deliberately writes.
    /// </summary>
    private TsModule Add(TsDeclaration declaration)
    {
        if (!_bound.Add((declaration.Space, declaration.Name)))
        {
            var what = declaration.Space == TsDeclarationSpace.Value ? "binding" : "type";

            throw new TypeScriptException($"the module already declares a {what} called '{declaration.Name}'");
        }

        _declarations.Add(declaration);

        return this;
    }

    /// <summary>
    /// The finished module, <paramref name="header"/> first. Omitting it takes the plainest do-not-edit
    /// notice there is, since anything built by this is generated; what a banner <em>says</em> is still the
    /// caller's, and <see cref="TsComment.Empty"/> is how a module opens with none.
    /// <para>
    /// Line endings are <c>\n</c> throughout rather than the platform's, so the same declarations produce the
    /// same bytes wherever the generator runs; otherwise regenerating on another machine rewrites every line.
    /// </para>
    /// </summary>
    public string Render(TsComment? header = null)
    {
        if (_declarations.Count == 0)
        {
            throw new TypeScriptException("the module declares nothing");
        }

        var builder = new StringBuilder();

        header ??= TsComment.Lines([Headers.Default]);
        header.Write(builder);

        // A blank line under the banner, and none where there is no banner: what follows is the module's
        // first declaration, not a continuation of the notice above it.
        if (!header.IsEmpty)
        {
            builder.Append('\n');
        }

        for (var index = 0; index < _declarations.Count; index++)
        {
            if (index > 0)
            {
                builder.Append('\n');
            }

            _declarations[index].Write(builder);
            builder.Append('\n');
        }

        return builder.ToString();
    }
}
