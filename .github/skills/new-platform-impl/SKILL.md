---
name: new-platform-impl
description: >
  Step-by-step guide for adding a new platform implementation to DotSerial.
  Use when asked to add support for a new OS, transport, or connection type.
---

# DotSerial New Platform Implementation Skill

Follow these steps precisely when adding a new platform implementation.

## Step 1 — Decide the implementation base

| Scenario | Base class |
|----------|-----------|
| New OS using native serial APIs | Extend `SerialPortBase` |
| New transport protocol (TCP, USB, BLE) | Implement `ISerialPort` directly |
| New Bluetooth variant | Extend `SerialPortBase` or implement directly |

## Step 2 — Create the implementation file

File path convention:
- `src/DotSerial/Internal/{Platform}/{Platform}SerialPort.cs`
- `src/DotSerial/Internal/{Platform}/{Platform}BluetoothSerialPort.cs` (if Bluetooth variant)

Use the copyright header. Class must be `internal sealed`.

## Step 3 — Add to csproj

Open `src/DotSerial/DotSerial.csproj` and add a `<Compile Remove>` block:

```xml
<!-- New platform — exclude from all other TFMs -->
<ItemGroup Condition="'$(TargetFramework)' != 'net10.0-{platform}'">
  <Compile Remove="Internal\{Platform}\{Platform}SerialPort.cs" />
</ItemGroup>
```

If the new platform requires a new TFM, also add it to `<TargetFrameworks>`.

## Step 4 — Wire into SerialPortFactory

In `SerialPortFactory.cs`, add a branch inside `Create()`:

```csharp
#elif {PLATFORM_SYMBOL}
return settings.ConnectionType == Enums.ConnectionType.Bluetooth
    ? new Internal.{Platform}.{Platform}BluetoothSerialPort(
        settings,
        _loggerFactory.CreateLogger<Internal.{Platform}.{Platform}BluetoothSerialPort>())
    : new Internal.{Platform}.{Platform}SerialPort(
        settings,
        _loggerFactory.CreateLogger<Internal.{Platform}.{Platform}SerialPort>());
```

Use the correct platform symbol (`ANDROID`, `IOS`, or define a new one via `<DefineConstants>`).

## Step 5 — Add to ConnectionType (if new type)

If the new platform introduces a new `ConnectionType`:
1. Add a value to `DotSerial.Enums.ConnectionType`
2. Add validation logic in `SerialPortSettings.Validate()`
3. Document it in `.github/copilot-instructions.md` under **ConnectionType and Bluetooth**

## Step 6 — Update GetPortNames and CreateMonitor

If the platform has port discovery or hotplug support, implement it in
`SerialPortFactory.GetPortNames()` and/or `SerialPortFactory.CreateMonitor()`.

## Step 7 — Write unit tests

Add tests to `tests/DotSerial.Tests.Unit/` that verify:
- Factory creates the correct type for the new platform (mock the platform check if needed)
- Settings validation for the new platform/connection type
- Basic `Open`/`Close`/`Write`/`Read` lifecycle (mock the hardware layer)

## Step 8 — Validate

```bash
dotnet build
dotnet test tests/DotSerial.Tests.Unit
```
