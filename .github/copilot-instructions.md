# DotSerial — Copilot Instructions

## Project Overview

DotSerial is a cross-platform .NET 10 serial port library supporting Windows, Linux, macOS, Android, and iOS.
Solution format: `.slnx` (`DotSerial.slnx`).

## Build & Test Commands

```bash
# Restore and build all projects
dotnet restore
dotnet build

# Build release
dotnet build -c Release

# Run unit tests
dotnet test tests/DotSerial.Tests.Unit

# Run unit tests with coverage
dotnet test tests/DotSerial.Tests.Unit --collect:"XPlat Code Coverage"

# Run integration tests (requires real/virtual COM port)
dotnet test tests/DotSerial.Tests.Integration

# Run all tests
dotnet test

# Pack NuGet package
dotnet pack src/DotSerial -c Release -o ./artifacts

# Run specific test
dotnet test --filter "FullyQualifiedName~SerialPortSettingsTests"
```

## Architecture

```
DotSerial.Abstractions   → ISerialPort, ISerialPortFactory, ISerialPortMonitor, ISerialPortStream (public interfaces)
DotSerial.Enums          → BaudRate, DataBits, Parity, StopBits, FlowControl, SerialError, SerialPinChange, SerialData, ConnectionType
DotSerial.Models         → SerialPortSettings (immutable record), event args, PortsChangedEventArgs
DotSerial.Exceptions     → SerialPortException (base), SerialPortNotFoundException, SerialPortTimeoutException
DotSerial.Internal       → Platform implementations (all internal visibility)
  ├── SerialPortBase     → Abstract base class — all platform impls except NetworkSerialPort extend this
  ├── Desktop            → DesktopSerialPort (Condition net10.0)
  ├── Linux              → LinuxSerialPortMonitor (Condition net10.0, OS-specific at runtime)
  ├── MacOS              → MacOSSerialPortMonitor (Condition net10.0, OS-specific at runtime)
  ├── Network            → NetworkSerialPort (all platforms; implements ISerialPort directly, not via SerialPortBase)
  ├── Android            → AndroidSerialPort, AndroidBluetoothSerialPort (Condition net10.0-android)
  └── iOS                → iOSSerialPort, iOSBluetoothSerialPort (Condition net10.0-ios)
DotSerial.Decorators     → ThrottledSerialPort (platform-agnostic write-rate limiter, wraps any ISerialPort)
DotSerial.Streams        → SerialPortStreamWrapper (wraps ISerialPort as ISerialPortStream)
DotSerial                → SerialPortFactory, ServiceCollectionExtensions
```

`SerialPortFactory.CreateMonitor()` selects the monitor at runtime: `LinuxSerialPortMonitor` on Linux, `MacOSSerialPortMonitor` on macOS, and the polling `DesktopSerialPortMonitor` on Windows. The `CreateStream()` method wraps any `ISerialPort` in `SerialPortStreamWrapper`.

## File Header Requirement

Every `.cs` file MUST start with:

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

## Namespace Structure

- `DotSerial` — `SerialPortFactory`, `ServiceCollectionExtensions`
- `DotSerial.Abstractions` — all interfaces
- `DotSerial.Models` — `SerialPortSettings`, event args
- `DotSerial.Enums` — all enums
- `DotSerial.Exceptions` — custom exceptions
- `DotSerial.Internal` — platform implementations (`internal` visibility)

## SOLID Conventions

- **S**: Each class has one responsibility (factory, port impl, settings validation)
- **O**: `ISerialPort` is open for extension via new implementations
- **L**: All `ISerialPort` implementations are substitutable
- **I**: Interface is focused; stream access is via `BaseStream`
- **D**: Consumers depend on `ISerialPortFactory` and `ISerialPort` abstractions

## Platform Preprocessor Symbols

The csproj uses `<Compile Remove>` with MSBuild `Condition` attributes (preferred) to exclude platform-specific files:
- `Condition="'$(TargetFramework)' == 'net10.0'"` — desktop only (Windows, Linux, macOS)
- `Condition="'$(TargetFramework)' == 'net10.0-android'"` — Android only
- `Condition="'$(TargetFramework)' == 'net10.0-ios'"` — iOS only

For **inline `#if` guards** in files that compile across all TFMs (e.g., `SerialPortFactory.cs`), use:
- `#if ANDROID` — Android only
- `#if IOS` — iOS only
- The `else` branch covers desktop (net10.0)

Do NOT use `NET10_0_ANDROID`/`NET10_0_IOS` in `#if` guards — the correct runtime symbols are `ANDROID` and `IOS`.

## ConnectionType and Bluetooth

`SerialPortSettings` has a `ConnectionType` property (`Serial` default, `Bluetooth`, `Network`) and an optional `BluetoothAddress`.

- **Desktop**: only `ConnectionType.Serial` and `ConnectionType.Network` are supported; `Bluetooth` throws `PlatformNotSupportedException`.
- **Android Bluetooth**: set `ConnectionType = Bluetooth`, `BluetoothAddress = "00:11:22:33:44:55"` (MAC), `PortName = device name`.
- **iOS Bluetooth**: set `ConnectionType = Bluetooth`, `BluetoothAddress = CBPeripheral UUID`, uses NUS BLE profile.
- **Network (all platforms)**: set `ConnectionType = Network`, `PortName = "host:port"` (e.g., `"192.168.1.100:4001"`). Serial line settings in `SerialPortSettings` are stored for reference only; configure them on the network serial server device.
- `Validate()` enforces `BluetoothAddress` is non-empty when `ConnectionType == Bluetooth`, and that `PortName` is `"host:port"` when `ConnectionType == Network`.

## ThrottledSerialPort Decorator

Token-bucket write throttle wrapping any `ISerialPort`:
```csharp
using var throttled = new ThrottledSerialPort(port, maxBytesPerSecond: 9600, logger);
throttled.Write(data, 0, data.Length); // blocked until rate allows
```
- Sync writes use `Thread.Sleep`; async writes use `Task.Delay`.
- All read operations and properties delegate unmodified to the inner port.
- Thread-safe via `SemaphoreSlim _bucketLock`.

## ISerialPortMonitor

Detects port hotplug/removal:
```csharp
using var monitor = factory.CreateMonitor(TimeSpan.FromSeconds(1));
monitor.PortsChanged += (_, e) => Console.WriteLine($"Added: {string.Join(", ", e.AddedPorts)}");
monitor.Start();
```
DI: `services.AddDotSerial().AddDotSerialMonitor();`

## NSubstitute Patterns for Tests

The test framework is **NUnit 4** + **NSubstitute 5**. Use `[TestFixture]`/`[SetUp]`/`[TearDown]`/`[Test]` attributes. Test files also carry the standard copyright header.

```csharp
// For logger factory, use NullLoggerFactory (simplest approach)
using Microsoft.Extensions.Logging.Abstractions;
var loggerFactory = new NullLoggerFactory();

// Mock ISerialPort
var port = Substitute.For<ISerialPort>();
port.IsOpen.Returns(true);
port.PortName.Returns("COM1");

// Mock ISerialPortFactory
var factory = Substitute.For<ISerialPortFactory>();
factory.Create(Arg.Any<SerialPortSettings>()).Returns(port);
```

The test project uses `InternalsVisibleTo("DotSerial.Tests.Unit")` (declared in the main csproj) so internal types can be tested directly.

## Project Conventions

- **Nullable reference types** enabled — handle nullability explicitly
- **Implicit usings** enabled — System, Collections.Generic, Linq, Threading, etc. available
- **TreatWarningsAsErrors** enabled — fix all warnings
- All public APIs require XML doc comments (`<summary>`, `<param>`, `<returns>`, `<exception>`)
- Use `ArgumentNullException.ThrowIfNull()` for null checks
- Use `ConfigureAwait(false)` on all `await` calls in library code

## Versioning

Versioning is managed by **MinVer** from the nearest git tag with `v` prefix (e.g., `v1.2.0`). Pre-release builds use the identifier `preview`. Do not manually set `<Version>` in csproj.

## Branch and Commit Conventions

Branch naming: `feature/xxx`, `fix/xxx`, `chore/xxx`

Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/):
- `feat:`, `fix:`, `docs:`, `chore:`, `test:`, `refactor:`
