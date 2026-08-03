using System.Collections;
using System.Reflection;
using System.Text.Json.Serialization;

namespace SimpleTypeScript.TypeGeneration;

/// <summary>
/// C# types as TypeScript declarations: an interface per shape, a string union per enum, following members
/// from a set of roots rather than being told what exists. A type that stops being reachable stops being
/// generated, and a nested one arrives without being named twice.
/// <para>
/// <b>It reads the shape that is serialized, not only the shape that is declared.</b> <c>[JsonIgnore]</c>
/// drops a member and <c>[JsonPropertyName]</c> renames it, because a generator that reads the C# alone
/// differs from the JSON in ways only a runtime notices.
/// </para>
/// </summary>
public sealed class TypeWalker
{
    /// <summary>
    /// Declared in name order. The order a walk reaches types in is the order the reflection API happens to
    /// return members, which is not a promise — sorting is what makes the module byte-stable across runs.
    /// </summary>
    private readonly SortedDictionary<string, Action<TsModule>> _declarations = new(StringComparer.Ordinal);

    private readonly Dictionary<Type, TsType> _references = [];
    private readonly Dictionary<Type, TsType> _leaves;
    private readonly NullabilityInfoContext _nullability = new();
    private readonly TypeWalkerOptions _options;

    /// <summary>A walk configured by <paramref name="options"/>, or by the defaults where none are given.</summary>
    public TypeWalker(TypeWalkerOptions? options = null)
    {
        _options = options ?? new TypeWalkerOptions();
        _leaves = new Dictionary<Type, TsType>(TypeMappings.Default);
        foreach (var (type, mapping) in _options.Mappings)
        {
            _leaves[type] = mapping;
        }
    }

    /// <summary>How many declarations the walk has produced.</summary>
    public int Count => _declarations.Count;

    /// <summary>Walks <paramref name="roots"/> and everything their members reach.</summary>
    public TypeWalker Add(IEnumerable<Type> roots)
    {
        foreach (var root in roots)
        {
            Reference(root);
        }

        return this;
    }

    /// <inheritdoc cref="Add(IEnumerable{Type})" />
    public TypeWalker Add(params Type[] roots) => Add((IEnumerable<Type>)roots);

    /// <summary>Adds what the walk found to <paramref name="module"/>, in name order.</summary>
    public void Declare(TsModule module)
    {
        foreach (var declare in _declarations.Values)
        {
            declare(module);
        }
    }

    /// <summary>
    /// The TypeScript for <paramref name="type"/>, declaring it first where it is one this walk describes.
    /// The reference is recorded before the members are walked, so a shape that reaches itself terminates.
    /// </summary>
    private TsType Reference(Type type)
    {
        if (_leaves.TryGetValue(type, out var leaf))
        {
            return leaf;
        }

        if (_references.TryGetValue(type, out var known))
        {
            return known;
        }

        if (type.IsEnum)
        {
            return DeclareEnum(type);
        }

        if (Element(type) is { } element)
        {
            return TsType.ArrayOf(Reference(element));
        }

        if (Values(type) is { } values)
        {
            return TsType.Record(TsType.String, Reference(values));
        }

        if (type.IsGenericType || type.IsPrimitive || type.IsPointer || type == typeof(object))
        {
            throw new GenerationException(
                $"{type.FullName} has no TypeScript shape; map it in {nameof(TypeWalkerOptions)}."
                + $"{nameof(TypeWalkerOptions.Mappings)} or keep it off the wire");
        }

        return DeclareInterface(type);
    }

    private TsType DeclareEnum(Type type)
    {
        var name = _options.Name(type);
        var reference = TsType.Of(name);
        _references[type] = reference;

        var members = Enum.GetNames(type)
            .Select(member => _options.EnumNamingPolicy?.ConvertName(member) ?? member)
            .Select(TsType.StringLiteral)
            .ToArray();

        var doc = _options.Documentation.For(type);
        _declarations[name] = module => module.TypeAlias(name, TsType.Union(members), doc);

        return reference;
    }

    private TsType DeclareInterface(Type type)
    {
        var name = _options.Name(type);
        var reference = TsType.Of(name);
        _references[type] = reference;

        var members = new List<TsMember>();
        _declarations[name] = module => module.Interface(name, members, _options.Documentation.For(type));

        foreach (var property in Serialized(type))
        {
            var shape = Nullable(property)
                ? TsType.Union([Reference(Unwrap(property.PropertyType)), TsType.Null])
                : Reference(property.PropertyType);

            members.Add(new TsMember(
                Name(property),
                shape,
                _options.Documentation.For(property),
                _options.ReadOnlyMembers));
        }

        return reference;
    }

    /// <summary>
    /// The properties the producer actually sends, in the order they are declared. <c>MetadataToken</c> is
    /// that order — <c>GetProperties</c> promises none — and it keeps a generated interface reading like the
    /// type it came from rather than as an alphabetized list.
    /// </summary>
    private static IEnumerable<PropertyInfo> Serialized(Type type) => type
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
        .Where(property => property.GetCustomAttribute<JsonIgnoreAttribute>() is null)
        .OrderBy(property => property.MetadataToken);

    private string Name(PropertyInfo property) =>
        property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
        ?? _options.MemberNamingPolicy?.ConvertName(property.Name)
        ?? property.Name;

    /// <summary>Whether the member can arrive as <c>null</c>, for a value type and a reference type alike.</summary>
    private bool Nullable(PropertyInfo property) =>
        System.Nullable.GetUnderlyingType(property.PropertyType) is not null
        || _nullability.Create(property).ReadState == NullabilityState.Nullable;

    private static Type Unwrap(Type type) => System.Nullable.GetUnderlyingType(type) ?? type;

    /// <summary>What the type is a sequence of, or <c>null</c> where it is not one.</summary>
    private static Type? Element(Type type)
    {
        if (Values(type) is not null)
        {
            return null;
        }

        if (type.IsArray)
        {
            return type.GetElementType();
        }

        return type.IsAssignableTo(typeof(IEnumerable))
            ? Implemented(type, typeof(IEnumerable<>))?.GetGenericArguments()[0]
            : null;
    }

    /// <summary>
    /// What the type maps its keys to, or <c>null</c> where it is not a dictionary. A JSON object's keys are
    /// strings whatever the C# key is, so only the value type reaches the output.
    /// </summary>
    private static Type? Values(Type type) =>
        Implemented(type, typeof(IDictionary<,>))?.GetGenericArguments()[1]
        ?? Implemented(type, typeof(IReadOnlyDictionary<,>))?.GetGenericArguments()[1];

    private static Type? Implemented(Type type, Type definition) =>
        (type.IsGenericType && type.GetGenericTypeDefinition() == definition ? type : null)
        ?? Array.Find(
            type.GetInterfaces(),
            candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == definition);
}
