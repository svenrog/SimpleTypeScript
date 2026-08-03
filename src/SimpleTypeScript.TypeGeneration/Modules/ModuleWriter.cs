namespace SimpleTypeScript.TypeGeneration.Modules;

/// <summary>
/// Builds a module, opens it with the banner, and writes it where the module says it belongs.
/// <para>
/// <b>Everything it can refuse is a <see cref="GenerationException"/></b>, including what the emitter refuses
/// underneath it — so a host has one thing to catch and one sentence to print, and the distinction between a
/// source that did not hold and a name TypeScript cannot spell stays available on
/// <see cref="Exception.InnerException"/> for whoever wants it.
/// </para>
/// </summary>
public sealed class ModuleWriter
{
    private readonly string _outputDirectory;
    private readonly GeneratedHeader _header;

    /// <summary>
    /// Writes under <paramref name="outputDirectory"/>, which has to exist: it is the consumer's own source
    /// tree, and creating it would mean generating a directory nobody reads instead of saying the path is
    /// wrong.
    /// </summary>
    public ModuleWriter(string outputDirectory, GeneratedHeader? header = null)
    {
        _outputDirectory = Path.GetFullPath(outputDirectory);
        _header = header ?? GeneratedHeader.Default;

        if (!Directory.Exists(_outputDirectory))
        {
            throw new GenerationException($"no such output directory '{_outputDirectory}'");
        }
    }

    /// <summary>Writes <paramref name="module"/>, returning where it went and what it said.</summary>
    public GeneratedFile Write(IGeneratedModule module)
    {
        var typescript = new TsModule();

        string summary;
        string rendered;
        try
        {
            summary = module.Build(typescript);
            rendered = typescript.Render(_header.For(module.Source));
        }
        catch (TypeScriptException ex)
        {
            throw new GenerationException(ex.Message, ex);
        }

        var destination = Path.Combine(_outputDirectory, module.FileName);
        var directory = Path.GetDirectoryName(destination)!;
        if (module.OwnsDirectory && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }

        Directory.CreateDirectory(directory);
        File.WriteAllText(destination, rendered);

        return new GeneratedFile(module.FileName, destination, summary);
    }

    /// <summary>Writes each of <paramref name="modules"/>, in the order given.</summary>
    public IReadOnlyList<GeneratedFile> WriteAll(IEnumerable<IGeneratedModule> modules) =>
        [.. modules.Select(Write)];
}
