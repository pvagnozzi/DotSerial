# Changelog

All notable changes to DotSerial are documented in this file.
Format follows [Keep a Changelog](https://keepachangelog.com/).

## [Unreleased]

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
