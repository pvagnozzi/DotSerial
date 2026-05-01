# Changelog

All notable changes to DotSerial are documented in this file.
Format follows [Keep a Changelog](https://keepachangelog.com/).

## [Unreleased]

### Added
- `NetworkSerialPort` — cross-platform TCP/IP-to-serial bridge (works on all platforms: Windows, Linux, macOS, Android, iOS); connect to Moxa, Lantronix, ATEN, socat serial servers by `"host:port"` PortName
- `SerialPortFactory.Create()` routes `ConnectionType.Network` to `NetworkSerialPort` on all platforms (takes priority over platform-specific branching)
- `ISerialPortStream` interface now extends `IAsyncDisposable` in addition to `IDisposable`
- `SerialPortStreamWrapper` — `ISerialPortStream` implementation wrapping any `ISerialPort` via `BaseStream`
- `ISerialPortFactory.CreateStream(SerialPortSettings)` factory method returning an `ISerialPortStream`
- `SerialPortFactory.CreateStream()` implementation — creates port via `Create()` then wraps in `SerialPortStreamWrapper`
- `SerialPortSettings.Validate()` now validates `host:port` format (port 1–65535) for `ConnectionType.Network`
- **MinVer** integration — package version is now derived automatically from the nearest git tag (e.g. `v1.2.3`); removes hardcoded `<Version>1.0.0</Version>`
- `MinVerTagPrefix=v` and `MinVerDefaultPreReleaseIdentifiers=preview` configured in project file
- Updated `publish.yml` — removed manual version extraction; MinVer sets version during `dotnet build`
- 37 new unit tests: `NetworkSerialPortTests` (21) and `SerialPortStreamWrapperTests` (16)
- Updated `PackageTags` to include `bluetooth` and `tcp`

### Added (previous unreleased)
- `ISerialPortMonitor` abstraction with `PortsChanged` event, `CurrentPorts`, `IsRunning`, `Start`/`Stop`/`StartAsync`
- `DesktopSerialPortMonitor` — polling-based implementation for Windows, Linux, macOS
- `ISerialPortFactory.CreateMonitor(TimeSpan?)` factory method
- `AddDotSerialMonitor()` DI extension for `IServiceCollection`
- `ThrottledSerialPort` decorator — token-bucket write throttling (bytes/second) wrapping any `ISerialPort`
- `ConnectionType` enum (`Serial`, `Bluetooth`, `Network`)
- `SerialPortSettings.ConnectionType` and `BluetoothAddress` properties
- `AndroidBluetoothSerialPort` — Android RFCOMM/SPP stub (Bluetooth MAC address via `PortName`)
- `iOSBluetoothSerialPort` — iOS Core Bluetooth (BLE NUS) stub (CBPeripheral UUID via `PortName`)
- `SerialPortFactory.Create()` routes Bluetooth settings to the correct platform stub
- 47 new unit tests covering monitor lifecycle, throttle behaviour, and Bluetooth validation

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
