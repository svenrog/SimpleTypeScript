namespace SimpleTypeScript.TypeGeneration.Modules;

/// <summary>
/// The comment every generated module opens with. Here rather than in the emitter, because nothing about a
/// do-not-edit notice is a fact about TypeScript — it is a fact about having been generated.
/// </summary>
public abstract class GeneratedHeader
{
    /// <summary>The header for a module generated from <paramref name="source"/>.</summary>
    public abstract TsComment For(string source);

    /// <summary>
    /// Names the generator by its own assembly, which is <see cref="AssemblyGeneratedHeader"/> over the entry
    /// assembly — what a build-time tool wants without saying so.
    /// </summary>
    public static GeneratedHeader Default { get; } = new AssemblyGeneratedHeader();

    /// <summary>No banner at all, for output that carries its provenance some other way.</summary>
    public static GeneratedHeader None { get; } = new NoHeader();

    private sealed class NoHeader : GeneratedHeader
    {
        public override TsComment For(string source) => TsComment.Lines([]);
    }
}
