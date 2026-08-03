# SimpleTypeScript

An emitter for generated TypeScript. Build a module out of declarations, types and values; the library
decides how each of them is spelled, once, so every generator writing through it produces the same shape.

```csharp
var module = new TsModule()
    .Const("LOCALES", TsValue.Array(["en-US", "sv-SE"]), asConst: true)
    .Const(
        "MESSAGES",
        TsValue.Object(new Dictionary<string, string> { ["greeting"] = "Hej då" }),
        TsType.Record(TsType.String, TsType.String),
        doc: "Every key, authored in the neutral culture.");

File.WriteAllText(path, module.Render(TsComment.Lines(["GENERATED — do not edit."])));
```

```ts
// GENERATED — do not edit.

export const LOCALES = ["en-US","sv-SE"] as const;

/** Every key, authored in the neutral culture. */
export const MESSAGES: Record<string, string> = {
  "greeting": "Hej då",
};
```

## What it is for

Generated code is compiled before anyone reads it, so a rule that is merely *probably* right produces a file
that is broken where nobody is looking. The rules that are easy to get wrong are the ones this owns:

- **String literals are escaped for ECMAScript source, not for JSON.** The two differ where it matters: JSON
  leaves U+2028 and U+2029 raw inside a string, ECMAScript ends a line on them. A lone surrogate is escaped
  for the same reason — it cannot be encoded as well-formed UTF-8. Everything else printable is written as
  itself, because a generated vocabulary is read by whoever checks a translation and `å` is no use to
  them.
- **A comment cannot be ended from inside it.** Text carrying a line terminator would put the rest of itself
  into the module as code, so every terminator re-opens the comment — including the two only ECMAScript
  treats as one — and a doc comment's `*/` is neutralised.
- **Numbers are invariant and round-trippable**, and `NaN` and the infinities are refused rather than emitted
  as something that parses and means another thing.
- **Output is byte-stable.** Line endings are `\n` whatever the platform spells a newline as, and order is
  the caller's, so regenerating on another machine does not rewrite every line of a file that did not change.

Nothing here reflects over a type: a value is built explicitly, so what is emitted is what the generator said
to emit, and no trimming or AOT caveat is carried into a consumer.

## The model

| Type | What it is |
| --- | --- |
| `TsModule` | A header comment, then exported declarations. Holds declarations rather than their text, so what a module *is* stays inspectable until the moment it is written. |
| `TsValue` | A value literal: `String`, `Number`, `Boolean`, `Null`, `Array`, `Object`. Objects always break across lines; arrays stay on one while everything inside fits. |
| `TsType` | An annotation: the primitives, `Record<,>`, `T[]`, and a named reference. |
| `TsComment` | `Lines` for a `//` header, `Doc` for the `/** … */` an editor surfaces at the use site. |
| `TsMember` | One property signature: a name, a type, `readonly`, a doc comment. |
| `TypeScriptException` | The refusal: a binding name that is not one, a number with no literal form, a type this model does not describe. |

Shapes as well as values, which is what generating DTOs from a wire contract needs:

```csharp
new TsModule()
    .TypeAlias("ScanStatus", TsType.Union(["Queued", "Running"].Select(TsType.StringLiteral)))
    .Interface(
        "ScanSummary",
        [
            new TsMember("id", TsType.String, isReadOnly: true),
            new TsMember("status", TsType.Of("ScanStatus")),
            new TsMember("finishedAt", TsType.Union([TsType.String, TsType.Of("null")])),
        ],
        doc: "One scan, as the API returns it.");
```

A union is the one type here that does not close with its own syntax, so `TsType` tracks whether a container
has to parenthesise it — `("a" | "b")[]` rather than `"a" | "b"[]`, which is a different type that compiles
just as well. That flag is the whole precedence model, deliberately.

**Deliberately not the whole type system.** Unions, intersections and conditionals have no shape to model
them from, and every level of a grammar that is modelled has to be kept correct. Every type here closes with
its own syntax, which is why nothing needs parenthesising and no precedence is tracked; a composed type
written as a name is refused by `TsType.Of` rather than half-supported.

## Generating from C# types

`SimpleTypeScript.TypeGeneration` is the companion package: give it roots, and it follows what their members
reach — an interface per shape, a string union per enum, everything written through the emitter above.

```csharp
var module = new TsModule();

new TypeWalker(new TypeWalkerOptions
    {
        Documentation = new XmlDocumentationSource(),
        Mappings = new Dictionary<Type, TsType> { [typeof(Money)] = TsType.String },
    })
    .Add(typeof(Order), typeof(Customer))
    .Declare(module);

File.WriteAllText(path, module.Render(TsComment.Lines(["GENERATED — do not edit."])));
```

**It reads the shape that is serialized, not only the shape that is declared.** `[JsonIgnore]` drops a
member, `[JsonPropertyName]` renames it, and everything else takes the naming policy — camel case by default,
matching what a JSON API is usually configured with. A generator that reads the C# alone spells every member
wrong the moment a policy is set, and nothing says so until a field is `undefined`.

The rest of what it decides, and how to overrule it:

| | |
| --- | --- |
| A nullable member | `T \| null`, for a nullable reference and a `Nullable<T>` alike |
| An enum | a union of the strings the wire carries — `EnumNamingPolicy` if a converter renames them, or map it to `TsType.Number` if it is serialized as one |
| A sequence | `T[]`; a dictionary is `Record<string, V>`, since a JSON key is a string whatever the C# key is |
| A BCL type | `TypeMappings.Default` — dates, `Guid`, `Uri`, every numeric, `JsonElement` as `unknown` |
| Anything else | refused rather than written as `any`, because a shape that checks nothing is not noticed |
| Doc comments | none, unless an `IDocumentationSource` is given; `XmlDocumentationSource` reads the compiler's and flattens it to a sentence |

A mapped type is a **leaf**: nothing behind it is reached, which is what makes mapping the fix for a
framework type dragging its own graph into the output. Declarations are emitted in name order and members in
declaration order, so the file is byte-stable across runs and machines.

This half reflects, so it is a package of its own — the emitter reflects over nothing, and a consumer
publishing NativeAOT keeps that by taking only the emitter.

## Installing

```
dotnet add package SimpleTypeScript
dotnet add package SimpleTypeScript.TypeGeneration   # only if you generate from C# types
```

`net10.0`.

## License

MIT. See [LICENSE.txt](LICENSE.txt).
