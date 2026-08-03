# SimpleTypeScript — architecture notes

Guidance for Claude Code in this repository. What the package is *for* is `README.md`; this is the *where*.

## Namespaces

**The root namespace is the API, and nothing else is in it.** A consumer writes one `using
SimpleTypeScript;` and reaches `TsModule`, `TsComment`, `TsType`, `TsValue`, `TsMember` and
`TypeScriptException` — the five things you build with and the one thing that refuses. Everything below is
`internal` and lives in a namespace named after the part of the grammar it belongs to:

| Namespace | What is in it |
| --- | --- |
| `SimpleTypeScript` | The public face: entry points, abstract bases, the factories that produce nodes. |
| `SimpleTypeScript.Declarations` | `TsDeclaration` and one class per declaration kind (`TsConst`, `TsInterface`, `TsTypeAlias`). |
| `SimpleTypeScript.Types` | The `TsType` nodes — named, array, generic, union, string literal. |
| `SimpleTypeScript.Values` | The `TsValue` nodes — raw, string, array, object. |
| `SimpleTypeScript.Syntax` | `TsSyntax`: escaping, identifiers, number form, indentation. |
| `SimpleTypeScript.TypeGeneration` | **A second project and package**: the walk (`TypeWalker`, `TypeWalkerOptions`, `TypeMappings`) and `GenerationException`. |
| `SimpleTypeScript.TypeGeneration.Documentation` | Where a doc comment comes from: the `IDocumentationSource` seam and its XmlDoc implementation. |
| `SimpleTypeScript.TypeGeneration.Modules` | The pipeline a multi-file generator would otherwise write for itself: `IGeneratedModule`, `TypeModule`, `ModuleCatalog`, `ModuleWriter`, `GeneratedHeader`. |

**Adding a construct is a class in the namespace for its grammar, plus a factory on the public base** — the
node never becomes public, because the constructors are `private protected` and that is what keeps every
string reaching a file escaped on the way. A new *kind* of thing (imports, a file graph, a mapping layer)
earns its own namespace under the root; a new instance of an existing kind does not.

The internals are reachable across namespaces without ceremony because `internal` is assembly-wide, and a
file in `SimpleTypeScript.Types` sees the root namespace without a `using` — it is a parent of its own.

**`TypeGeneration` is an assembly boundary, not just a namespace**, and the line is reflection: the emitter
reflects over nothing and its csproj claims AOT compatibility for it, so the walk cannot live beside it
without taking that claim away from every consumer. Anything that reads `Type`, `PropertyInfo` or an
attribute belongs on that side; anything that decides how TypeScript is spelled belongs on this one.

## Not here yet

**Imports.** A generator emitting one type per file needs `import type { X } from "./x";`, which is a
declaration kind plus a file graph — two files, not one. Nothing has needed it: a single module holding every
declaration needs no imports at all, which is what let a consumer retire a second generator without one.

## Invariants

- **A value is built, never serialized.** Nothing reflects over a type, which is what keeps the package
  AOT-clean for a consumer that publishes one.
- **Escaping is for ECMAScript source, not JSON** (`TsSyntax`), and the two differ where it matters: U+2028,
  U+2029 and a lone surrogate.
- **A comment cannot be closed from inside it**, on every terminator ECMAScript recognises.
- **Output is byte-stable**: `\n` endings, caller-decided order, invariant numbers.
- **Precedence is one flag.** `TsType.RequiresParentheses` exists for unions and nothing else. A second
  construct needing it is the point to write a real precedence rank rather than a second boolean.
- **A refusal is a `TypeScriptException`**, thrown rather than emitted — generated code is compiled before
  anyone reads it.

## Tests

`tests/SimpleTypeScript.Tests` asserts what a reader would otherwise have to reason about: the escaping
corpus, the quoting rules, the parenthesising, and every refusal. A new construct ships its tests in the same
file as its kind (`TypeScriptEmitterTests` for values, `TypeScriptShapeTests` for shapes).

## Style

`.editorconfig` and `Directory.Build.props` carry it (`TreatWarningsAsErrors`, XmlDoc on declarations, private
fields `_camelCase`, one top-level class per file). `dotnet format --verify-no-changes` runs in CI.
