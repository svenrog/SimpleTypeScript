using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
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
/// <para>
/// <b>One walk is one thread's.</b> It accumulates what it has reached, and the nullability context it reads
/// annotations through is not thread-safe either — a generator producing several modules at once gives each
/// its own.
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
    private readonly Dictionary<string, Type> _named = [];
    private readonly Dictionary<Type, TsType> _leaves;
    private readonly NullabilityInfoContext _nullability = new();
    private readonly TypeWalkerOptions _options;

    /// <summary>A walk configured by <paramref name="options"/>, or by the defaults where none are given.</summary>
    public TypeWalker(TypeWalkerOptions? options = null)
    {
        _options = options ?? new TypeWalkerOptions();

        // The same value JsonSerializerOptions refuses, for the same reason: as everything's default it
        // describes a producer that writes no member at all.
        if (_options.DefaultIgnoreCondition == JsonIgnoreCondition.Always)
        {
            throw new GenerationException(
                $"{nameof(TypeWalkerOptions)}.{nameof(TypeWalkerOptions.DefaultIgnoreCondition)} cannot be "
                + $"{nameof(JsonIgnoreCondition.Always)}; a member ignored unconditionally says so itself");
        }

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
    /// The TypeScript for one position — what stands there, and <c>null</c> where the annotation says it may
    /// be absent. A position rather than a type, because whether null belongs is a property of where the
    /// type is used: the same <c>string</c> is nullable inside one list and not inside the next.
    /// </summary>
    private TsType At(Type type, NullabilityInfo? nullability)
    {
        var shape = Reference(Unwrap(type), nullability);

        return IsNullable(type, nullability) ? TsType.Union([shape, TsType.Null]) : shape;
    }

    /// <summary>
    /// The TypeScript for <paramref name="type"/>, declaring it first where it is one this walk describes.
    /// The reference is recorded before the members are walked, so a shape that reaches itself terminates.
    /// <para>
    /// <paramref name="nullability"/> is what the annotation said about this position, carried only so the
    /// positions inside it — an element, a dictionary's value — can be asked about in turn.
    /// </para>
    /// </summary>
    private TsType Reference(Type type, NullabilityInfo? nullability = null)
    {
        if (_leaves.TryGetValue(type, out var leaf))
        {
            return leaf;
        }

        if (_references.TryGetValue(type, out var known))
        {
            return known;
        }

        // After the mapped types, so a caller that wants one written its own way still wins, and before
        // everything structural, which would otherwise meet the wrapper rather than what is in it.
        if (System.Nullable.GetUnderlyingType(type) is { } underlying)
        {
            return TsType.Union([Reference(underlying), TsType.Null]);
        }

        if (type.IsEnum)
        {
            return DeclareEnum(type);
        }

        // A dictionary is a sequence of its own pairs, so it is asked about first; what follows would
        // otherwise answer for it with an array of KeyValuePair.
        if (Values(type) is { } values)
        {
            return TsType.Record(TsType.String, At(values, Argument(nullability, 1)));
        }

        if (Element(type) is { } element)
        {
            return TsType.ArrayOf(At(element, ElementOf(type, nullability)));
        }

        if (type.IsGenericType || type.IsPrimitive || type.IsPointer || type == typeof(object)
            || IsPlatform(type))
        {
            throw new GenerationException(
                $"{type.FullName} has no TypeScript shape; map it in {nameof(TypeWalkerOptions)}."
                + $"{nameof(TypeWalkerOptions.Mappings)} or keep it off the wire");
        }

        return DeclareInterface(type);
    }

    /// <summary>
    /// Records that <paramref name="type"/> is the one written as <paramref name="name"/>. Two types under
    /// one name would leave a single declaration with both of them referring to it, so every member of one
    /// would be checked against the shape of the other — silently, since the module still compiles.
    /// </summary>
    private void Claim(string name, Type type)
    {
        if (_named.TryGetValue(name, out var taken) && taken != type)
        {
            throw new GenerationException(
                $"{taken.FullName} and {type.FullName} are both written as '{name}'; tell them apart in "
                + $"{nameof(TypeWalkerOptions)}.{nameof(TypeWalkerOptions.Name)} or keep one off the wire");
        }

        _named[name] = type;
    }

    private TsType DeclareEnum(Type type)
    {
        var name = _options.Name(type);
        Claim(name, type);

        var reference = TsType.Of(name);
        _references[type] = reference;

        var doc = _options.Documentation.For(type);
        var names = Names(type).ToArray();

        // One entry writing two declarations, for the style that needs both: they stay adjacent and sort
        // under one name, which is what keeps a value and the type read off it from drifting apart.
        _declarations[name] = _options.EnumStyle switch
        {
            EnumStyle.NumberUnion => module => module.TypeAlias(name, TsType.Union(Numbers(type)), doc),
            EnumStyle.ConstObject => module => module
                .Const(name, TsValue.AsConst(TsValue.Object(names)), doc: doc)
                .TypeAlias(name, TsType.ValuesOf(name)),
            _ => module => module.TypeAlias(
                name,
                TsType.Union(names.Select(entry => TsType.StringLiteral(entry.Value))),
                doc),
        };

        return reference;
    }

    /// <summary>
    /// Each member as the name a consumer spells it by and the string the wire carries it as. The two differ
    /// wherever a naming policy is in play, and only the second is comparable against a received value.
    /// </summary>
    private IEnumerable<KeyValuePair<string, string>> Names(Type type) => Enum
        .GetNames(type)
        .Select(member => KeyValuePair.Create(member, _options.EnumNamingPolicy?.ConvertName(member) ?? member));

    /// <summary>
    /// The underlying values, distinctly. Two members may share one — an alias is a second name for a value,
    /// not a second value — and a union repeating it says the same thing twice.
    /// </summary>
    private static IEnumerable<TsType> Numbers(Type type) => Enum
        .GetValues(type)
        .Cast<object>()
        .Select(value => Convert.ToDouble(value, CultureInfo.InvariantCulture))
        .Distinct()
        .Select(TsType.NumberLiteral);

    private TsType DeclareInterface(Type type)
    {
        var name = _options.Name(type);
        Claim(name, type);

        var reference = TsType.Of(name);
        _references[type] = reference;

        var members = new List<TsMember>();
        _declarations[name] = module => module.Interface(name, members, _options.Documentation.For(type));

        foreach (var property in Serialized(type))
        {
            // Read once and carried down: the context walks the member's whole annotation tree, and the
            // positions inside the type are read off the same one the member itself was.
            var nullability = _nullability.Create(property);

            // A member the producer omits rather than sends empty is absent, not null: the condition that
            // takes the key out is the same one that keeps a null from ever arriving in it.
            var omitted = Omitted(property, nullability);

            var shape = Reference(Unwrap(property.PropertyType), nullability);
            if (!omitted && IsNullable(property.PropertyType, nullability))
            {
                shape = TsType.Union([shape, TsType.Null]);
            }

            members.Add(new TsMember(Name(property), shape)
            {
                Doc = _options.Documentation.For(property),
                IsReadOnly = _options.ReadOnlyMembers,
                IsOptional = omitted,
            });
        }

        return reference;
    }

    /// <summary>
    /// The properties the producer actually sends, in the order they are declared. <c>MetadataToken</c> is
    /// that order — <c>GetProperties</c> promises none — and it keeps a generated interface reading like the
    /// type it came from rather than as an alphabetized list.
    /// </summary>
    private List<PropertyInfo> Serialized(Type type)
    {
        var serialized = new List<PropertyInfo>();

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.CanRead
                && property.GetIndexParameters().Length == 0
                && !IsUnwritten(property)
                && !IsExtensionData(property))
            {
                serialized.Add(property);
            }
        }

        // A token is a property's own, so there are no ties for an unstable sort to reorder.
        serialized.Sort(static (left, right) => left.MetadataToken.CompareTo(right.MetadataToken));

        return serialized;
    }

    /// <summary>
    /// Whether the producer never writes the member at all, which is a member the payload does not have.
    /// <c>WhenReading</c> is the other way round — ignored on the way in and written on the way out — so it
    /// stays.
    /// </summary>
    private bool IsUnwritten(PropertyInfo property)
    {
        var condition = Condition(property);

#if NET10_0_OR_GREATER
        // The two conditions naming a direction arrived in .NET 10, and so did the serializer that honours
        // them: before it, a producer has no way to be configured this way for the walk to describe.
        if (condition is JsonIgnoreCondition.WhenWriting)
        {
            return true;
        }
#endif

        return condition is JsonIgnoreCondition.Always;
    }

    /// <summary>
    /// Whether the member is the bag the producer flattens rather than a member of its own. The serializer
    /// writes its entries as members of the object holding it, under whatever keys it turns out to have —
    /// so what is declared stays right and stops being everything the payload can carry, which no shape
    /// written ahead of time could have said anyway.
    /// </summary>
    private static bool IsExtensionData(PropertyInfo property) =>
        property.IsDefined(typeof(JsonExtensionDataAttribute), inherit: true);

    /// <summary>
    /// When the producer leaves the member out: its own <c>[JsonIgnore]</c> where it carries one, and what
    /// the producer is configured to do otherwise. The condition is the whole of what the attribute says —
    /// unconditional by default, but <see cref="JsonIgnoreCondition.Never"/> means the opposite of the
    /// attribute's own name, and the two writing conditions mean sometimes.
    /// </summary>
    private JsonIgnoreCondition Condition(PropertyInfo property) =>
        property.IsDefined(typeof(JsonIgnoreAttribute), inherit: true)
            ? property.GetCustomAttribute<JsonIgnoreAttribute>()!.Condition
            : _options.DefaultIgnoreCondition;

    /// <summary>
    /// Whether the member can arrive absent, which is what <c>?</c> says. Sometimes-omitted is a member that
    /// is present and optional rather than one that is gone: what decides it is the condition, and a value
    /// the condition would leave out.
    /// </summary>
    private bool Omitted(PropertyInfo property, NullabilityInfo nullability)
    {
        var omits = Condition(property) switch
        {
            JsonIgnoreCondition.WhenWritingNull => IsNullable(property.PropertyType, nullability),

            // Anything can equal its own default, except a reference the type says is never null: the
            // default there is null, so a value that arrives at all is one the producer writes.
            JsonIgnoreCondition.WhenWritingDefault =>
                IsNullable(property.PropertyType, nullability) || property.PropertyType.IsValueType,

            _ => false,
        };

        // Asked only where it can change the answer, which is three attribute lookups a member nothing
        // would leave out has no reason to pay.
        return omits && !IsRequired(property);
    }

    /// <summary>
    /// Whether the API refuses a payload without the member. Three attributes say so and only the last is
    /// the serializer's own to enforce — but a member published as required is one a consumer is answered
    /// with an error for omitting, so none of them may be written optional.
    /// <para>
    /// It says nothing about the <em>value</em>: <c>[Required]</c> is validation, which runs on the way in
    /// and leaves what is written alone, so a member the C# declares nullable stays <c>T | null</c>.
    /// </para>
    /// </summary>
    private static bool IsRequired(PropertyInfo property) =>
        property.IsDefined(typeof(RequiredAttribute), inherit: true)
        || property.IsDefined(typeof(RequiredMemberAttribute), inherit: true)
        || property.IsDefined(typeof(JsonRequiredAttribute), inherit: true);

    private string Name(PropertyInfo property) =>
        property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
        ?? _options.MemberNamingPolicy?.ConvertName(property.Name)
        ?? property.Name;

    /// <summary>
    /// Whether the position can hold <c>null</c>, for a value type and a reference type alike. The first
    /// half is in the type and the second only in the annotation, which is why the annotation has to reach
    /// wherever the type does.
    /// </summary>
    private static bool IsNullable(Type type, NullabilityInfo? nullability) =>
        System.Nullable.GetUnderlyingType(type) is not null
        || nullability?.ReadState == NullabilityState.Nullable;

    /// <summary>
    /// What the annotation said about the type argument at <paramref name="index"/>, or <c>null</c> where it
    /// said nothing — a type that reaches its element through an interface rather than its own arguments,
    /// which leaves the elements unannotated rather than annotated as non-null.
    /// </summary>
    private static NullabilityInfo? Argument(NullabilityInfo? nullability, int index) =>
        nullability is not null && nullability.GenericTypeArguments.Length > index
            ? nullability.GenericTypeArguments[index]
            : null;

    /// <summary>What the annotation said about what the sequence holds. An array keeps it apart from its arguments.</summary>
    private static NullabilityInfo? ElementOf(Type type, NullabilityInfo? nullability) =>
        type.IsArray ? nullability?.ElementType : Argument(nullability, 0);

    private static Type Unwrap(Type type) => System.Nullable.GetUnderlyingType(type) ?? type;

    /// <summary>
    /// Whether the type belongs to the platform rather than to the producer. Nothing here is a DTO, so
    /// walking one describes an implementation instead of a payload — an interface of whatever properties
    /// the framework happens to expose, which compiles, checks nothing and is wrong in a way the output
    /// gives no sign of. A platform type carried on the wire is a mapping, which is where the caller says
    /// what it is carried as.
    /// </summary>
    private static bool IsPlatform(Type type)
    {
        if (type.Namespace is not { } declared)
        {
            return false;
        }

        return declared == "System"
            || declared.StartsWith("System.", StringComparison.Ordinal)
            || declared.StartsWith("Microsoft.", StringComparison.Ordinal);
    }

    /// <summary>What the type is a sequence of, or <c>null</c> where it is not one.</summary>
    private static Type? Element(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        return type.IsAssignableTo(typeof(IEnumerable))
            ? Implemented(type, type.GetInterfaces(), typeof(IEnumerable<>))?.GetGenericArguments()[0]
            : null;
    }

    /// <summary>
    /// What the type maps its keys to, or <c>null</c> where it is not a dictionary. A JSON object's keys are
    /// strings whatever the C# key is, so only the value type reaches the output.
    /// </summary>
    private static Type? Values(Type type)
    {
        // Both questions are asked of one array: every read of it is a fresh one.
        var interfaces = type.GetInterfaces();

        return Implemented(type, interfaces, typeof(IDictionary<,>))?.GetGenericArguments()[1]
            ?? Implemented(type, interfaces, typeof(IReadOnlyDictionary<,>))?.GetGenericArguments()[1];
    }

    private static Type? Implemented(Type type, Type[] interfaces, Type definition)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == definition)
        {
            return type;
        }

        foreach (var candidate in interfaces)
        {
            if (candidate.IsGenericType && candidate.GetGenericTypeDefinition() == definition)
            {
                return candidate;
            }
        }

        return null;
    }
}
