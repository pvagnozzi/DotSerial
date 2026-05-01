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
DotSerial.Abstractions   → ISerialPort, ISerialPortFactory, ISerialPortStream (public interfaces)
DotSerial.Enums          → BaudRate, DataBits, Parity, StopBits, FlowControl, SerialError, SerialPinChange, SerialData
DotSerial.Models         → SerialPortSettings (immutable record), event args classes
DotSerial.Exceptions     → SerialPortException (base), SerialPortNotFoundException, SerialPortTimeoutException
DotSerial.Internal       → Platform implementations (internal visibility)
  ├── Desktop            → DesktopSerialPort (#if NET10_0) — wraps System.IO.Ports.SerialPort
  ├── Android            → AndroidSerialPort (#if NET10_0_ANDROID) — USB Host Mode stub
  └── iOS                → iOSSerialPort (#if NET10_0_IOS) — External Accessory stub
DotSerial                → SerialPortFactory (public), ServiceCollectionExtensions
```

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

- `NET10_0` — desktop only (Windows, Linux, macOS) — bare net10.0 TFM
- `NET10_0_ANDROID` — Android only
- `NET10_0_IOS` — iOS only

The `#if NET10_0` correctly excludes Android/iOS because those TFMs define `NET10_0_ANDROID`/`NET10_0_IOS` but NOT `NET10_0`.

## NSubstitute Patterns for Tests

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

## Project Conventions

- **Nullable reference types** enabled — handle nullability explicitly
- **Implicit usings** enabled — System, Collections.Generic, Linq, Threading, etc. available
- **TreatWarningsAsErrors** enabled — fix all warnings
- All public APIs require XML doc comments (`<summary>`, `<param>`, `<returns>`, `<exception>`)
- Use `ArgumentNullException.ThrowIfNull()` for null checks
- Use `ConfigureAwait(false)` on all `await` calls in library code
