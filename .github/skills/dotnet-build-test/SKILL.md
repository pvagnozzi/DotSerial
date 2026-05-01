---
name: dotnet-build-test
description: >
  Build and test the DotSerial project. Use when asked to build, run tests, check CI
  status, or verify that changes don't break the build or test suite.
allowed-tools: shell
---

# DotSerial Build and Test Skill

When asked to build or test DotSerial, follow this process:

## 1. Restore dependencies (if needed)

```bash
dotnet restore
```

## 2. Build (check for zero warnings — TreatWarningsAsErrors is on)

```bash
dotnet build
```

A clean build must produce **zero errors and zero warnings**.

## 3. Run unit tests

```bash
dotnet test tests/DotSerial.Tests.Unit
```

## 4. Run a specific test class or method

```bash
dotnet test --filter "FullyQualifiedName~{ClassName}"
# Example:
dotnet test --filter "FullyQualifiedName~SerialPortSettingsTests"
```

## 5. Run with coverage report

```bash
dotnet test tests/DotSerial.Tests.Unit --collect:"XPlat Code Coverage"
```

## 6. Run all tests (unit + integration)

> Integration tests require a real or virtual COM port — skip on machines without one.

```bash
dotnet test
```

## Interpreting results

- If the build fails with warnings: fix every warning (they are errors here).
- If a test fails: read the failure message, identify the failing test class, and investigate the relevant source file.
- Do NOT suppress warnings with `#pragma warning disable` or `<NoWarn>` unless absolutely necessary and documented.
