# SimpleTypeScript 📘

[![Platform](https://img.shields.io/badge/Platform-.NET%2010-blue.svg?style=flat)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/SimpleTypeScript)](https://www.nuget.org/packages/SimpleTypeScript)
[![License: MIT](https://img.shields.io/github/license/svenrog/SimpleTypeScript)](https://github.com/svenrog/SimpleTypeScript/blob/master/LICENSE.txt)

An emitter for generated TypeScript. Build a module out of declarations, types and values; the library
decides how each of them is spelled, once, so every generator writing through it produces the same shape.

```csharp
var module = new TsModule()
    .Const("LOCALES", TsValue.AsConst(TsValue.Array(["en-US", "sv-SE"])))
    .Const(
        "MESSAGES",
        TsValue.Object(new Dictionary<string, string> { ["greeting"] = "Hej värld" }),
        TsType.Record(TsType.String, TsType.String),
        doc: "Every key, authored in the neutral culture.");

File.WriteAllText(path, module.Render());
```

```ts
// GENERATED — do not edit.

export const LOCALES = ["en-US","sv-SE"] as const;

/** Every key, authored in the neutral culture. */
export const MESSAGES: Record<string, string> = {
  "greeting": "Hej värld",
};
```

Shapes as well as values:

```csharp
string[] statuses = ["Queued", "Running"];

var module = new TsModule()
    .TypeAlias("ScanStatus", TsType.Union(statuses.Select(TsType.StringLiteral)))
    .Interface(
        "ScanSummary",
        [
            new TsMember("id", TsType.String) { IsReadOnly = true },
            new TsMember("status", TsType.Of("ScanStatus")),
            new TsMember("finishedAt", TsType.Union([TsType.String, TsType.Null])) { Doc = "Null while running." },
        ],
        doc: "One scan, as the API returns it.");
```

```ts
export type ScanStatus = "Queued" | "Running";

/** One scan, as the API returns it. */
export interface ScanSummary {
  readonly id: string;
  status: ScanStatus;
  /** Null while running. */
  finishedAt: string | null;
}
```

Generating those declarations **from C# types** is
[`SimpleTypeScript.TypeGeneration`](https://www.nuget.org/packages/SimpleTypeScript.TypeGeneration), a
companion package. This one has no dependencies and reflects over nothing.

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

Nothing here reflects: a value is built explicitly, so what is emitted is what the generator said to emit,
and no trimming or AOT caveat is carried into a consumer.

## The model

| Type | What it is |
| --- | --- |
| `TsModule` | A header comment, then exported declarations. Holds declarations rather than their text, so what a module *is* stays inspectable until the moment it is written. |
| `TsValue` | A value literal: `String`, `Number`, `Boolean`, `Null`, `Array`, `Object`. Objects always break across lines; arrays stay on one while everything inside fits. |
| `TsType` | An annotation: the primitives, `Null`, `Record<,>`, `T[]`, a named reference, and a union of any of them. |
| `TsMember` | One property signature: a name, a type, `readonly`, a doc comment. |
| `TsComment` | `Lines` for a `//` header, `Doc` for the `/** … */` an editor surfaces at the use site, `Empty` for a module that opens with no banner at all. |
| `TypeScriptException` | The refusal: a binding name that is not one, a number with no literal form, an interface with no members. |

A union is the one type here that does not close with its own syntax, so `TsType` tracks whether a container
has to parenthesise it — `("a" | "b")[]` rather than `"a" | "b"[]`, which is a different type that compiles
just as well. That flag is the whole precedence model, deliberately.

**Deliberately not the whole type system.** Intersections, conditionals and generics beyond `Record` and the
array have no shape here, because every level of a grammar that is modelled has to be kept correct. A
composed type written as a *name* is refused by `TsType.Of` rather than half-supported: a union is something
to build, not something to spell.

## Installing

```
dotnet add package SimpleTypeScript
```

`net8.0` and `net10.0`.

## License

MIT. See [LICENSE.txt](https://github.com/svenrog/SimpleTypeScript/blob/master/LICENSE.txt).
