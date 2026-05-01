# 🔌 DotSerial

[![NuGet](https://img.shields.io/nuget/v/DotSerial.svg?color=blue&logo=nuget)](https://www.nuget.org/packages/DotSerial/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/DotSerial.svg)](https://www.nuget.org/packages/DotSerial/)
[![CI](https://github.com/pvagnozzi/DotSerial/actions/workflows/ci.yml/badge.svg)](https://github.com/pvagnozzi/DotSerial/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/pvagnozzi/DotSerial/branch/main/graph/badge.svg)](https://codecov.io/gh/pvagnozzi/DotSerial)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)

**DotSerial** is a modern, cross-platform .NET serial port library that works identically on **Windows**, **Linux**, **macOS**, **Android**, and **iOS**. It provides a clean interface for both classic byte-level I/O and stream-based communication, with built-in logging and full dependency injection support.

## ✨ Features

- 🌍 **Cross-platform** — Windows, Linux, macOS, Android, iOS
- 🔄 **Classic & Stream I/O** — `Write`/`Read` and `Stream`-based access
- ⚡ **Async-first** — full `async`/`await` support with `CancellationToken`
- 📋 **Dependency Injection** — works with `Microsoft.Extensions.DependencyInjection`
- 📝 **Logging** — built-in `Microsoft.Extensions.Logging` integration
- 🧪 **Testable** — interface-based design; fully mockable
- 🎯 **SOLID** — clean abstractions, single-responsibility, open for extension

## 📦 Installation

```bash
dotnet add package DotSerial
```

## 🚀 Quick Start

### Classic I/O

```csharp
using DotSerial;
using DotSerial.Models;

var factory = new SerialPortFactory(LoggerFactory.Create(b => b.AddConsole()));

using var port = factory.Create(new SerialPortSettings
{
    PortName = "COM3",
    BaudRate = 115200,
    DataBits = 8,
});

port.Open();
port.WriteLine("Hello, Serial!");
string response = port.ReadLine();
port.Close();
```

### Stream-based I/O

```csharp
port.Open();
var stream = port.BaseStream;

using var writer = new StreamWriter(stream, leaveOpen: true);
using var reader = new StreamReader(stream, leaveOpen: true);

await writer.WriteLineAsync("AT");
string? reply = await reader.ReadLineAsync();
```

### Dependency Injection

```csharp
builder.Services.AddLogging();
builder.Services.AddDotSerial();

public class MyService(ISerialPortFactory factory)
{
    public void Send(string portName, string data)
    {
        using var port = factory.Create(new SerialPortSettings { PortName = portName });
        port.Open();
        port.WriteLine(data);
    }
}
```

## 🖥️ Supported Platforms

| Platform | Support | Notes |
|----------|---------|-------|
| Windows  | ✅ Full | COM1…COMn via `System.IO.Ports` |
| Linux    | ✅ Full | /dev/ttyUSB*, /dev/ttyS* |
| macOS    | ✅ Full | /dev/cu.* |
| Android  | 🚧 Planned | USB Host Mode, requires `android.permission.USB_PERMISSION` |
| iOS      | 🚧 Planned | External Accessory (MFi accessories only) |

## 📡 Events

```csharp
port.DataReceived  += (s, e) => Console.WriteLine($"Data: {e.EventType}");
port.ErrorReceived += (s, e) => Console.WriteLine($"Error: {e.ErrorType}");
port.PinChanged    += (s, e) => Console.WriteLine($"Pin: {e.EventType}");
```

## 🔍 Port Discovery

```csharp
var factory = new SerialPortFactory(loggerFactory);
var ports = factory.GetPortNames();
foreach (var name in ports) Console.WriteLine(name);
```

## 🧪 Running Tests

```bash
# Unit tests
dotnet test tests/DotSerial.Tests.Unit

# Single test
dotnet test tests/DotSerial.Tests.Unit --filter "FullyQualifiedName~SerialPortSettingsTests"

# Integration tests (requires a real or virtual COM port)
dotnet test tests/DotSerial.Tests.Integration
```

## 🤝 Contributing

Please read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request.

## 📄 License

MIT © [Piergiorgio Vagnozzi](https://github.com/pvagnozzi)
