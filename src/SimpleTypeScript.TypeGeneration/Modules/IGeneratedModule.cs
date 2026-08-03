namespace SimpleTypeScript.TypeGeneration.Modules;

/// <summary>
/// One generated TypeScript module. Adding a generator is implementing this and nothing else —
/// <see cref="ModuleCatalog"/> finds it and <see cref="ModuleWriter"/> owns the banner, the file write and
/// what is reported, so a module states only what is its own.
/// </summary>
public interface IGeneratedModule
{
    /// <summary>
    /// The path written under the output directory, e.g. <c>localization/i18n.generated.ts</c>. A path rather
    /// than a file name: generated modules usually sit beside the hand-written ones that read them, so the
    /// directory may not exist on a clean checkout.
    /// </summary>
    string FileName { get; }

    /// <summary>
    /// What the banner names as this module's source, as a phrase completing "Source: …". A description of
    /// what was read rather than a list of paths, because a path is the thing that stops being true.
    /// </summary>
    string Source { get; }

    /// <summary>
    /// Whether everything in the module's directory is this module's, and so may be emptied before the write.
    /// Off by default: a generated module usually sits beside hand-written files, and there the directory
    /// belongs to the consumer. On, it is what keeps a file the generator has stopped producing from staying
    /// importable — the stale shape being the one nobody notices.
    /// </summary>
    bool OwnsDirectory => false;

    /// <summary>
    /// Appends this module's declarations to <paramref name="module"/> and returns the one line an operator
    /// sees — its own count of whatever it generated, since only the module knows what it measures. Throws
    /// <see cref="GenerationException"/> when a declaration it reads does not hold.
    /// </summary>
    string Build(TsModule module);
}
