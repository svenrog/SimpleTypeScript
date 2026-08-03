using System.Reflection;
using System.Reflection.Emit;

namespace SimpleTypeScript.Tests.Performance;

/// <summary>
/// A type graph of a chosen size, emitted rather than written out.
/// <para>
/// The walk's cost is a function of how many types and members it reaches, and the question worth asking is
/// what happens between one size and four times it. A fixture written by hand fixes that size at whatever a
/// person was willing to type, which is well below where a generator starts to hurt.
/// </para>
/// </summary>
internal static class SyntheticTypes
{
    private static readonly Dictionary<(int, int), Type[]> _built = [];

    /// <summary>
    /// <paramref name="count"/> types of <paramref name="members"/> properties each. Every type points at the
    /// next, so the graph is reached by walking one root rather than by being handed the list — which is the
    /// shape a real root has.
    /// <para>
    /// Cached: emitting is slower than walking, and a measurement that included it would report the emit.
    /// </para>
    /// </summary>
    public static Type[] Graph(int count, int members = 8)
    {
        if (_built.TryGetValue((count, members), out var cached))
        {
            return cached;
        }

        var module = AssemblyBuilder
            .DefineDynamicAssembly(new AssemblyName($"Synthetic{count}x{members}"), AssemblyBuilderAccess.RunAndCollect)
            .DefineDynamicModule("Main");

        var builders = new TypeBuilder[count];
        for (var index = 0; index < count; index++)
        {
            builders[index] = module.DefineType($"Shape{index}", TypeAttributes.Public | TypeAttributes.Class);
        }

        for (var index = 0; index < count; index++)
        {
            for (var member = 0; member < members; member++)
            {
                // Every fourth member points at the next type, so the walk has to follow rather than only
                // read; the rest are the primitives a DTO is mostly made of.
                var type = member % 4 == 3 && index + 1 < count
                    ? builders[index + 1]
                    : (member % 3) switch
                    {
                        0 => typeof(string),
                        1 => typeof(int),
                        _ => typeof(DateTimeOffset),
                    };

                Property(builders[index], $"Member{member}", type);
            }
        }

        var built = builders.Select(builder => builder.CreateType()).ToArray();
        _built[(count, members)] = built;

        return built;
    }

    private static void Property(TypeBuilder owner, string name, Type type)
    {
        var field = owner.DefineField($"_{name}", type, FieldAttributes.Private);
        var property = owner.DefineProperty(name, PropertyAttributes.None, type, null);

        var getter = owner.DefineMethod(
            $"get_{name}",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            type,
            null);

        var il = getter.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, field);
        il.Emit(OpCodes.Ret);

        property.SetGetMethod(getter);
    }
}
