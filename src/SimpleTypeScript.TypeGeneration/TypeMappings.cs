using System.Text.Json;
using System.Text.Json.Nodes;

namespace SimpleTypeScript.TypeGeneration;

/// <summary>
/// The types a walk stops at, each spelled as what the other side already reads it as.
/// <para>
/// <b>A mapping is a leaf.</b> Nothing behind a mapped type is reached, which makes mapping the fix for a
/// framework type dragging its own graph into the output rather than a workaround for one — and the way to
/// say that a type is carried as something other than its shape, which is what every date and identifier
/// here is.
/// </para>
/// </summary>
public static class TypeMappings
{
    /// <summary>
    /// What <c>System.Text.Json</c> writes for the BCL types a DTO is usually made of. A number is every
    /// numeric type — JSON has one — and a byte array is the base64 string it is serialized as, not the
    /// sequence it is in C#.
    /// </summary>
    public static IReadOnlyDictionary<Type, TsType> Default { get; } = new Dictionary<Type, TsType>
    {
        [typeof(string)] = TsType.String,
        [typeof(char)] = TsType.String,
        [typeof(bool)] = TsType.Boolean,
        [typeof(Guid)] = TsType.String,
        [typeof(Uri)] = TsType.String,
        [typeof(DateTime)] = TsType.String,
        [typeof(DateTimeOffset)] = TsType.String,
        [typeof(DateOnly)] = TsType.String,
        [typeof(TimeOnly)] = TsType.String,
        [typeof(TimeSpan)] = TsType.String,
        [typeof(byte[])] = TsType.String,
        [typeof(byte)] = TsType.Number,
        [typeof(sbyte)] = TsType.Number,
        [typeof(short)] = TsType.Number,
        [typeof(ushort)] = TsType.Number,
        [typeof(int)] = TsType.Number,
        [typeof(uint)] = TsType.Number,
        [typeof(long)] = TsType.Number,
        [typeof(ulong)] = TsType.Number,
        [typeof(float)] = TsType.Number,
        [typeof(double)] = TsType.Number,
        [typeof(decimal)] = TsType.Number,

        // A payload the producer declined to type. `unknown` rather than `any`: the consumer has to narrow it
        // before reading it, which is the same thing the C# says.
        [typeof(JsonElement)] = TsType.Of("unknown"),
        [typeof(JsonDocument)] = TsType.Of("unknown"),
        [typeof(JsonNode)] = TsType.Of("unknown"),
        [typeof(JsonObject)] = TsType.Of("unknown"),
        [typeof(JsonArray)] = TsType.Of("unknown"),
        [typeof(JsonValue)] = TsType.Of("unknown"),
    };
}
