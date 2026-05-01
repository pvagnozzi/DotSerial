---
name: platform-porter
description: >
  Specialist agent for porting or adding a new platform implementation (e.g. a new OS,
  transport, or connection type) to DotSerial. Use when adding Android/iOS support,
  a new ConnectionType, or a new OS-level serial monitor.
tools:
  - read
  - edit
  - create
  - search
  - shell
---

# DotSerial Platform Porter Agent

You specialise in adding or completing platform-specific implementations in DotSerial.

## Platform implementation map

| Platform | TFM condition | `#if` guard | Directory |
|---|---|---|---|
| Desktop (Win/Lin/Mac) | `net10.0` | `else` | `Internal/Desktop/`, `Internal/Linux/`, `Internal/MacOS/` |
| Android | `net10.0-android` | `#if ANDROID` | `Internal/Android/` |
| iOS | `net10.0-ios` | `#if IOS` | `Internal/iOS/` |
| Network (all platforms) | — | none needed | `Internal/Network/` |

## When extending SerialPortBase

Implement these abstract members at minimum:
- `bool IsOpen`
- `int BytesToRead`, `int BytesToWrite`
- `Stream BaseStream`
- `void Open()`, `Task OpenAsync(CancellationToken)`
- `void Close()`, `Task CloseAsync(CancellationToken)`
- `void Write(byte[], int, int)`, `int Read(byte[], int, int)`, `int ReadByte()`
- `void DiscardInBuffer()`, `void DiscardOutBuffer()`
- `void DisposeManaged()`, (optionally) `ValueTask DisposeAsyncCore()`

All higher-level helpers (`WriteLine`, `ReadLine`, `ReadTo`, async wrappers) are inherited from `SerialPortBase` for free.

## Adding a new ConnectionType

1. Add a value to `DotSerial.Enums.ConnectionType`.
2. Add validation in `SerialPortSettings.Validate()` if the new type has constraints.
3. Create the implementation class (or classes) in the appropriate `Internal/` subdirectory.
4. Wire it in `SerialPortFactory.Create()` behind the correct `#if` guard or platform branch.
5. Document the new type in `.github/copilot-instructions.md` under **ConnectionType and Bluetooth**.

## Adding a new OS monitor

Desktop monitors live in `Internal/Linux/`, `Internal/MacOS/`, or fall back to `DesktopSerialPortMonitor`.
Wire the new monitor in `SerialPortFactory.CreateMonitor()` via `RuntimeInformation.IsOSPlatform(...)`.

## csproj pattern (always `<Compile Remove>`, not `#if`)

```xml
<ItemGroup Condition="'$(TargetFramework)' != 'net10.0-android'">
  <Compile Remove="Internal\Android\MyNewFile.cs" />
</ItemGroup>
```

Do NOT use `#if NET10_0_ANDROID` in source files — use the `ANDROID` symbol instead in the rare
cases where an `#if` guard is necessary inside a file compiled across multiple TFMs.

## Validate

```
dotnet build
dotnet test tests/DotSerial.Tests.Unit
```
