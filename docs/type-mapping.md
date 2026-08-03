# Plan: declare types, not just values

Today the emitter writes **values** — `export const X = …` — which is everything a generated vocabulary,
palette or locale table needs. What it cannot write is a **shape**: `export interface`, `export type`, and
the string-literal union that a C# enum becomes on a JSON wire.

That gap is why a consumer generating DTOs still runs a second, third-party generator beside this one. Adding
three declaration kinds closes it, and the whole addition is grammar this model already almost describes.

## What is added

### 1. `TsInterface` — an exported shape

```ts
/** A queued scan, as the API returns it. */
export interface ScanSummary {
  id: string;
  domain: string;
  status: ScanStatus;
  finishedAt: string | null;
  findings: Finding[];
}
```

A declaration holding an ordered list of members, each a name, a `TsType`, an optional doc comment and a
`readonly` flag. Reached as `module.Interface("ScanSummary", members, doc: …)`, beside the existing
`module.Const(…)`.

**One open decision: whether a member name is quoted.** `TsObjectValue` quotes every key on purpose — uniform
quoting is what a reader scanning a generated *map* wants, and the rules for a bare property name are not the
rules for a binding. An interface is read as *code* rather than scanned as data, so the argument does not
carry over cleanly. Proposed: bare when `TsSyntax.IsIdentifier` passes (it is already conservative), quoted
otherwise — one rule, no new lexical surface.

### 2. `TsTypeAlias` — an exported name for a type

```ts
export type ScanStatus = "Queued" | "Running" | "Completed" | "Failed";
```

`module.TypeAlias("ScanStatus", type, doc: …)`. Needed for the union below, and it is what a C# enum becomes
when the wire carries member names as strings.

### 3. `TsType.Union` and `TsType.StringLiteral` — and the one invariant they cost

`TsType`'s XmlDoc says every type it carries closes with its own syntax, which is why nothing is
parenthesised and no precedence is tracked. **A union is the first type that does not close**: `"a" | "b"`
inside an array is `("a" | "b")[]`, and emitting `"a" | "b"[]` is a different type that still compiles.

So the addition is two things, not one:

- `TsType.StringLiteral(string)` — a literal type, escaped through `TsSyntax` like any other string.
- `TsType.Union(IEnumerable<TsType>)` — rendered `a | b | c`, and carrying a `RequiresParentheses` flag that
  `TsArrayType` honours. That flag is the whole of the precedence model, and it stays that way: the moment a
  second construct needs it, the right move is a real precedence rank rather than a second boolean.

`TsType.Of` keeps refusing a composed type written as a *name* — a union is now something you build, not
something you spell.

### 4. Deferred: imports

A generator emitting one type per file needs `import type { X } from "./x";`, and that is a fourth
declaration kind plus a file-graph the emitter does not model. **Not in this plan.** A single module holding
every interface needs no imports at all, and a consumer that wants one file per type can render one module
per file and add the import kind then.

## Tests

The existing suite asserts what a reader would otherwise have to reason about, and these are the same shape:

- An interface with a member whose name is not an identifier, quoted; one that is, bare.
- A union inside an array parenthesised; a union alias not.
- A union member escaped for source (the ECMAScript separators, a lone surrogate) — the same corpus the
  string tests already use, since a literal type *is* a string literal.
- An interface declaring nothing, and a union of nothing — both refused with `TypeScriptException` rather
  than emitted as `{}` and an empty type.
- The byte-stability fact extended to a module of interfaces: `\n` endings, caller-decided order.

## Version

Additive — nothing existing changes shape, so `0.2.0`.
