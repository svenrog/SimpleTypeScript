using System.Reflection;
using System.Runtime.CompilerServices;

namespace SimpleTypeScript.TypeGeneration.Modules;

/// <summary>
/// Every <see cref="IGeneratedModule"/> an assembly declares. Discovered rather than listed, so adding a
/// generator is one class and no edit to a registry — there is no second roster to keep in step.
/// </summary>
public static class ModuleCatalog
{
    /// <summary>
    /// The modules <paramref name="assembly"/> declares — the calling one where none is given — ordered by
    /// file name, so a run reports and writes in the same order every time and a failure in one module does
    /// not depend on which others happen to exist.
    /// <para>
    /// Internal types are included: a module is an implementation detail of the generator that declares it,
    /// and making one public to be found would be the discovery telling the consumer how to write its code.
    /// </para>
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IReadOnlyList<IGeneratedModule> From(Assembly? assembly = null)
    {
        var subject = assembly ?? Assembly.GetCallingAssembly();

        var modules = subject
            .GetTypes()
            .Where(type => typeof(IGeneratedModule).IsAssignableFrom(type))
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .Select(Create)
            .OrderBy(module => module.FileName, StringComparer.Ordinal)
            .ToArray();

        if (modules.Length == 0)
        {
            throw new GenerationException($"{subject.GetName().Name} declares no generated modules, so a run would write nothing");
        }

        var duplicate = modules
            .GroupBy(module => module.FileName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new GenerationException(
                $"two modules both write '{duplicate.Key}', so one would silently overwrite the other");
        }

        return modules;
    }

    /// <summary>
    /// A module, which has to be constructible without arguments for discovery to mean anything. A module
    /// needing more than that is one the consumer instantiates and hands to <see cref="ModuleWriter"/>
    /// itself.
    /// </summary>
    private static IGeneratedModule Create(Type type) =>
        Activator.CreateInstance(type, nonPublic: true) as IGeneratedModule
        ?? throw new GenerationException($"{type.Name} is a generated module with no parameterless constructor");
}
