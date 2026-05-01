---
applyTo: "src/**/Internal/**/*.cs"
---

# DotSerial Internal implementation conventions

These rules apply specifically to files under `src/**/Internal/`.

## Visibility

All classes in `Internal/` must be `internal sealed` — never `public`.
Interfaces are implemented explicitly only when disambiguation is needed; prefer implicit.

## SerialPortBase vs direct ISerialPort

- Extend `SerialPortBase` when the implementation uses native serial APIs or `System.IO.Ports`.
- Implement `ISerialPort` directly (without `SerialPortBase`) only for transport-level abstractions
  that have different lifecycle semantics (e.g., `NetworkSerialPort` uses TCP, not `SerialPortBase`).

## Abstract members you MUST override when extending SerialPortBase

```csharp
public override bool IsOpen { get; }
public override int BytesToRead { get; }
public override int BytesToWrite { get; }
public override Stream BaseStream { get; }
public override void Open() { }
public override Task OpenAsync(CancellationToken ct = default) => ...;
public override void Close() { }
public override Task CloseAsync(CancellationToken ct = default) => ...;
public override void Write(byte[] buffer, int offset, int count) { }
public override int Read(byte[] buffer, int offset, int count) => ...;
public override int ReadByte() => ...;
public override void DiscardInBuffer() { }
public override void DiscardOutBuffer() { }
protected override void DisposeManaged() { }
```

## Logging

Use the injected `_logger` field from `SerialPortBase`. Log:
- `LogInformation` on `Open`/`Close`
- `LogDebug` on data transfer (if trace-level logging is appropriate)
- `LogError` on exceptions before rethrowing

## Event helpers

Raise events through the protected helpers:
```csharp
OnDataReceived(new SerialDataReceivedEventArgs(SerialData.Chars));
OnErrorReceived(new SerialErrorReceivedEventArgs(SerialError.Frame));
OnPinChanged(new SerialPinChangedEventArgs(SerialPinChange.CtsChanged));
```

## Guards

Call `ThrowIfDisposed()` and `ThrowIfNotOpen()` (inherited from `SerialPortBase`) at the start
of every operation that requires an open, non-disposed port.
