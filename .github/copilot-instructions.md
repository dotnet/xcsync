# Instructions for AIs

This repository is xcsync, it contains the code for a dotnet tool to synchronize a .NET iOS,macOS or tvOS project with an Xcode project for the purpose of creating Actions and Outlets for xib and storyboards.

This is the main branch targeting .NET 8.

## Nullable Reference Types

When opting C# code into nullable reference types:

Only make the following changes when asked to do so.

* Don't *ever* use `!` to handle `null`!

* Declare variables non-nullable, and check for `null` at entry points.

* Use `throw new ArgumentNullException (nameof (parameter))` in `netstandard2.0` projects.

* Use `ArgumentNullException.ThrowIfNull (parameter)` in iOS projects that will be .NET 9+.

* `[Required]` properties in MSBuild task classes should always be non-nullable with a default value.

* Non-`[Required]` properties should be nullable and have null-checks in C# code using them.

* For MSBuild task properties like:

```csharp
public string NonRequiredProperty { get; set; }
public ITaskItem [] NonRequiredItemGroup { get; set; }

[Output]
public string OutputProperty { get; set; }
[Output]
public ITaskItem [] OutputItemGroup { get; set; }

[Required]
public string RequiredProperty { get; set; }
[Required]
public ITaskItem [] RequiredItemGroup { get; set; }
```

Fix them such as:

```csharp
public string? NonRequiredProperty { get; set; }
public ITaskItem []? NonRequiredItemGroup { get; set; }

[Output]
public string? OutputProperty { get; set; }
[Output]
public ITaskItem []? OutputItemGroup { get; set; }

[Required]
public string RequiredProperty { get; set; } = "";
[Required]
public ITaskItem [] RequiredItemGroup { get; set; } = [];
```

If you see a `string.IsNullOrEmpty()` check, don't change it.

* Namespaces should be declared using a single line, e.g.:
```csharp
namespace MyNamespace;
```

* New files should be created with the copyright header:

```csharp
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

```

If you see an existing file without a copyright header, add the copyright header to the top of the file.

* Do not add unused namespaces, and if you see namespaces that aren't used, remove them.


## Formatting

C# code uses tabs (not spaces) and the code-formatting style defined in `.editorconfig`

* Your mission is to make diffs as absolutely as small as possible, preserving existing code formatting.

* If you encounter additional spaces or formatting within existing code blocks, LEAVE THEM AS-IS.

* If you encounter code comments, LEAVE THEM AS-IS.

* Place a space prior to any parentheses `(` or `[`

* Use `string.Empty` for empty string and *not* `""`

* Use `[]` for empty arrays and *not* `Array.Empty<T>()`

Examples of properly formatted code:

```csharp
Foo ();
Bar (1, 2, "test");
myarray [0] = 1;

if (someValue) {
    // Code here
}

try {
    // Code here
} catch (Exception e) {
    // Code here
}
```