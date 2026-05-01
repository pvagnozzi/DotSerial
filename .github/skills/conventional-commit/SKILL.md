---
name: conventional-commit
description: >
  Format a Conventional Commits message for DotSerial changes. Use when asked
  to write a commit message, format a commit, or check if a message follows
  the project's commit conventions.
---

# DotSerial Conventional Commit Skill

DotSerial follows [Conventional Commits 1.0](https://www.conventionalcommits.org/en/v1.0.0/).

## Format

```
<type>(<scope>): <subject>

[optional body]

[optional footer(s)]
```

## Allowed types

| Type | When to use |
|------|-------------|
| `feat` | New feature visible to library consumers |
| `fix` | Bug fix |
| `docs` | Documentation only (README, XML doc, CHANGELOG) |
| `chore` | Build, CI, tooling, dependency updates |
| `test` | Adding or fixing tests |
| `refactor` | Code change that neither adds a feature nor fixes a bug |
| `perf` | Performance improvement |

## Allowed scopes (optional but recommended)

| Scope | Meaning |
|-------|---------|
| `desktop` | `Internal/Desktop/` code |
| `android` | `Internal/Android/` code |
| `ios` | `Internal/iOS/` code |
| `network` | `Internal/Network/` code |
| `monitor` | `ISerialPortMonitor` and implementations |
| `settings` | `SerialPortSettings` |
| `factory` | `SerialPortFactory` |
| `throttle` | `ThrottledSerialPort` decorator |
| `stream` | `SerialPortStreamWrapper` / `ISerialPortStream` |
| `di` | `ServiceCollectionExtensions` |
| `ci` | GitHub Actions workflows |
| `release` | Release and versioning chores |

## Breaking changes

Add `!` after the type/scope, or add a `BREAKING CHANGE: <description>` footer:

```
feat(settings)!: make PortName required via `required` keyword

BREAKING CHANGE: SerialPortSettings.PortName is now a required init-only property.
Any object initializer that omits PortName will fail to compile.
```

## Examples

```
feat(network): add NetworkSerialPort for TCP/IP serial bridging
fix(desktop): handle null PortName in DesktopSerialPortMonitor.Start
chore(ci): upgrade to .NET 10.0.x in GitHub Actions
test(settings): add validation tests for ConnectionType.Network
docs: update README with Bluetooth Android usage example
refactor(throttle): replace Thread.Sleep with Task.Delay in sync path
```
