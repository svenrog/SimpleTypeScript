namespace SimpleTypeScript.TypeGeneration.Modules;

/// <summary>
/// A module whose content is a <see cref="TypeWalker"/> over a set of roots — the common case, so a consumer
/// generating DTOs says what its roots are and stops there.
/// <para>
/// There is no base class for the other kind. A module that builds its declarations by hand implements
/// <see cref="IGeneratedModule"/> directly: the only default there is belongs to
/// <see cref="IGeneratedModule.OwnsDirectory"/>, and a base class adding nothing else would be ceremony
/// between a generator and the two members it has to write anyway.
/// </para>
/// </summary>
public abstract class TypeModule : IGeneratedModule
{
    /// <inheritdoc />
    public abstract string FileName { get; }

    /// <inheritdoc />
    public abstract string Source { get; }

    /// <inheritdoc />
    public virtual bool OwnsDirectory => false;

    /// <summary>The types the walk starts from. What their members reach arrives without being named here.</summary>
    protected abstract IEnumerable<Type> Roots { get; }

    /// <summary>How the walk reads them. The defaults where a generator has nothing to say about it.</summary>
    protected virtual TypeWalkerOptions Options => new();

    /// <inheritdoc />
    public string Build(TsModule module)
    {
        var walker = new TypeWalker(Options).Add(Roots);
        walker.Declare(module);

        return $"{walker.Count} type(s)";
    }
}
