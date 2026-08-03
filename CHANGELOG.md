# Changelog

All notable changes to this project are documented here.

## 0.1.0

First release. The emitter behind a generated-module pipeline that was writing TypeScript out of C#
declarations, extracted once it had proved it knows nothing of the product it was written in:

- `TsModule`, `TsValue`, `TsType` and `TsComment` — a module held as declarations rather than as text, so
  what it *is* stays inspectable until it is written.
- String literals escaped for ECMAScript source rather than JSON, which is where U+2028, U+2029 and a lone
  surrogate stop being the serializer's problem.
- Comments that cannot be closed from inside, on every line terminator ECMAScript recognises.
- Numbers written invariant and round-trippable, with the three values that have no literal form refused.
- `\n` endings and caller-decided ordering, so the same declarations produce the same bytes on any machine.
