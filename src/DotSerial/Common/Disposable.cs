// -----------------------------------------------------------------------
// <copyright file="Disposable.cs" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     Base class that implements both synchronous and asynchronous dispose patterns.
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------

namespace DotSerial.Common;

/// <summary>
/// Base class that provides a complete implementation of <see cref="IDisposable"/>
/// and <see cref="IAsyncDisposable"/>.
/// </summary>
public abstract class Disposable : IDisposable, IAsyncDisposable
{
    private bool _disposed;

    /// <summary>
    /// Gets a value indicating whether this instance has already been disposed.
    /// </summary>
    protected bool IsDisposed => _disposed;

    /// <summary>
    /// Releases managed and unmanaged resources synchronously.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Dispose(true);
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases managed and unmanaged resources asynchronously.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(false);
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases managed and unmanaged resources.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> when called from <see cref="Dispose()"/>;
    /// <see langword="false"/> when called from finalization or async dispose path.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
    }

    /// <summary>
    /// Releases managed resources asynchronously.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous cleanup operation.</returns>
    protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;

    /// <summary>
    /// Throws <see cref="ObjectDisposedException"/> when this instance has already been disposed.
    /// </summary>
    protected void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);    
}
