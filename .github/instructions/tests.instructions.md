---
applyTo: "tests/**/*.cs"
---

# DotSerial test file conventions

These rules apply to all `.cs` files under `tests/`.

## File header

Tests still require the standard copyright header (same as source files).

## Framework

- **NUnit 4** attributes: `[TestFixture]`, `[SetUp]`, `[TearDown]`, `[Test]`, `[TestCase]`
- **NSubstitute 5** for mocking interfaces
- **NullLoggerFactory** (from `Microsoft.Extensions.Logging.Abstractions`) for logger dependencies
- XML doc comments are **not required** (`CS1591` is suppressed in the test csproj)

## Naming

Test method names follow: `MethodName_Scenario_ExpectedOutcome`

## Structure

```csharp
[TestFixture]
public sealed class FooTests
{
    private ILoggerFactory _loggerFactory = null!;

    [SetUp]
    public void SetUp() => _loggerFactory = new NullLoggerFactory();

    [TearDown]
    public void TearDown() => _loggerFactory.Dispose();
}
```

## NSubstitute key patterns

```csharp
var port = Substitute.For<ISerialPort>();
port.IsOpen.Returns(true);
port.Received(1).Open();          // verify call was made
port.DidNotReceive().Close();     // verify call was NOT made
```

## Assert style

Use constraint-model assertions: `Assert.That(x, Is.EqualTo(y))`.
For exceptions: `Assert.Throws<TException>(() => ...)` and `Assert.ThrowsAsync<TException>(async () => ...)`.

## Internals access

The test project has `InternalsVisibleTo` access to `DotSerial` — internal classes can be
instantiated and tested directly without any reflection tricks.
