---
applyTo: "src/**/*.cs"
---

# DotSerial source file conventions

Every `.cs` file in `src/` must follow these rules.

## Mandatory file header (first block, no exceptions)

```csharp
// -----------------------------------------------------------------------
// <copyright file="{FILENAME}" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     {ONE LINE SUMMARY}
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------
```

## Null checks

Use `ArgumentNullException.ThrowIfNull(param)` at every public and internal method entry.
Do not use manual `if (x == null) throw`.

## Async

Every `await` in library code must have `.ConfigureAwait(false)`.

## XML doc on all public members

Required elements: `<summary>`, `<param>` for each parameter, `<returns>` when non-void,
`<exception>` for documented exceptions.

## Internal platform implementations

- Platform-specific classes must be `internal sealed`.
- All platform classes that use native serial APIs must extend `DotSerial.Internal.SerialPortBase`.
- Cross-platform classes (e.g. NetworkSerialPort) may implement `ISerialPort` directly.
- Inline `#if` guards use `ANDROID` and `IOS` — NOT `NET10_0_ANDROID` or `NET10_0_IOS`.
- Prefer `<Compile Remove>` in the csproj over `#if` guards in source files.

## Disposal pattern

- Implement `IDisposable` via `DisposeManaged()` (abstract in `SerialPortBase`).
- Implement `IAsyncDisposable` via `DisposeAsyncCore()` (virtual, default calls `DisposeManaged`).
- Call `GC.SuppressFinalize(this)` in both `Dispose()` and `DisposeAsync()`.
- Guard re-entry with a `bool _disposed` field.
