namespace SimpleTypeScript.TypeGeneration.Modules;

/// <summary>What one module produced: where it was written, and its own account of what is in it.</summary>
/// <param name="FileName">The module's path under the output directory.</param>
/// <param name="Path">The absolute path written.</param>
/// <param name="Summary">The module's one line about what it generated.</param>
public sealed record GeneratedFile(string FileName, string Path, string Summary);
