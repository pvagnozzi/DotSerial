# Changelog

All notable changes to DotSerial are documented in this file.
Format follows [Keep a Changelog](https://keepachangelog.com/).

## [1.1.0] - 2026-05-01

### Added
- `NetworkSerialPort` — cross-platform TCP/IP-to-serial bridge; connect to Moxa, Lantronix, ATEN, socat serial servers by `"host:port"` PortName
- `SerialPortFactory.Create()` routes `ConnectionType.Network` to `NetworkSerialPort` on all platforms
- `ISerialPortStream` interface (extends `IAsyncDisposable` and `IDisposable`)
- `SerialPortStreamWrapper` — wraps any `ISerialPort` via `BaseStream`
- `ISerialPortFactory.CreateStream(SerialPortSettings)` factory method
- `ISerialPortMonitor` abstraction with `PortsChanged` event, `CurrentPorts`, `IsRunning`, `Start`/`Stop`/`StartAsync`
- `DesktopSerialPortMonitor` — polling-based implementation for Windows
- `LinuxSerialPortMonitor` — inotify-based implementation for Linux
- `MacOSSerialPortMonitor` — kqueue-based implementation for macOS
- `ISerialPortFactory.CreateMonitor(TimeSpan?)` factory method; `AddDotSerialMonitor()` DI extension
- `ThrottledSerialPort` decorator — token-bucket write throttling (bytes/second)
- `ConnectionType` enum (`Serial`, `Bluetooth`, `Network`)
- `SerialPortSettings.ConnectionType` and `BluetoothAddress` properties
- `AndroidBluetoothSerialPort` — Android RFCOMM/SPP Bluetooth stub
- `iOSBluetoothSerialPort` — iOS Core Bluetooth (BLE NUS) stub
- **MinVer** integration — version derived automatically from git tag (`v` prefix, `preview` pre-release)
- 84 new unit tests (Network, Stream wrapper, monitor lifecycle, throttle, Bluetooth validation)
- GitHub Copilot CLI configuration (agents, skills, hooks, MCP, plugin, LSP)

### Fixed
- `DesktopSerialPort.Open()`: catches `UnauthorizedAccessException` wrapping `IOException` on Linux/macOS when port does not exist, converting to `SerialPortNotFoundException`
- `SerialPortFactory.CreateMonitor()`: validates `pollingInterval` before platform dispatch — `ArgumentOutOfRangeException` thrown for zero/negative on all platforms
- CI: Android workload installed on all runners; iOS workload installed only on macOS
- `DotSerial.csproj`: `net10.0-ios` TFM excluded on non-macOS via MSBuild `IsOSPlatform` condition

## [1.0.0] - 2026-05-01
### Added
- Initial release
- `ISerialPort` interface with sync and async read/write
- `SerialPortFactory` with platform detection
- Desktop implementation (Windows, Linux, macOS) via `System.IO.Ports`
- Android and iOS platform stubs
- `IServiceCollection` extension (`AddDotSerial`)
- Built-in `Microsoft.Extensions.Logging` support
- NUnit + NSubstitute test suite
