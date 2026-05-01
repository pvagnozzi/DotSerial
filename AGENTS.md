# DotSerial — Agent Instructions

This file provides primary instructions for AI agents working on the DotSerial repository.
These instructions augment `.github/copilot-instructions.md`.

## Non-negotiable rules

- Every `.cs` file **must** begin with the exact copyright/summary header (see `.github/copilot-instructions.md`).
- All public types and members **must** have XML doc comments (`<summary>`, `<param>`, `<returns>`, `<exception>`).
- `TreatWarningsAsErrors` is enabled — the build **must** stay green; fix all warnings before committing.
- Use `ArgumentNullException.ThrowIfNull()` for every null-check at method entry points.
- Use `ConfigureAwait(false)` on every `await` in library code.

## Before committing any change

1. Run `dotnet build` — must succeed with zero warnings.
2. Run `dotnet test tests/DotSerial.Tests.Unit` — must pass.
3. Run `dotnet test --filter "FullyQualifiedName~<TestClass>"` when touching a specific area.

## Adding a new class

1. Choose the correct namespace and project folder (see Architecture in `.github/copilot-instructions.md`).
2. If it is a platform implementation: extend `SerialPortBase` (in `DotSerial.Internal`), mark it `internal sealed`.
3. If it is a cross-platform implementation (like `NetworkSerialPort`): implement `ISerialPort` directly, mark it `internal sealed`.
4. Register it in `SerialPortFactory.Create()` behind the appropriate `#if ANDROID`/`#if IOS`/`else` guard.
5. Add the file to the correct `<Compile Remove>` block in `DotSerial.csproj` if it is platform-specific.

## Adding a new test

- Mirror the source structure under `tests/DotSerial.Tests.Unit/`.
- Use `NullLoggerFactory` for logger dependencies.
- Use NSubstitute `Substitute.For<T>()` for interfaces.
- Test class attribute: `[TestFixture]`; setup: `[SetUp]`; teardown: `[TearDown]`; test method: `[Test]`.
- The test project has `InternalsVisibleTo` access to the main assembly — internal types are directly testable.

## Conventional commits

Format: `type(scope): description`  
Types: `feat`, `fix`, `docs`, `chore`, `test`, `refactor`  
Example: `feat(network): add TCP serial port reconnect support`
