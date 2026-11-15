using System.Text;
using UnrealSharp.Interop;

namespace UnrealSharp;

/// <summary>Profiling utilities for managed code.</summary>
public static class Profiler
{
    /// <summary>Begins a named Unreal Insights CPU trace scope for synchronous work on the game thread.</summary>
    /// <remarks>
    /// Use <c>using (Profiler.TraceScope("Gameplay.FindTargets"))</c> to close the scope,
    /// including when an exception occurs. Scopes must end in reverse creation order and
    /// must not cross await, thread switches or managed hot reload. When CPU tracing is
    /// disabled, the scope does not emit events.
    /// </remarks>
    /// <param name="name">A nonempty name of at most 1024 UTF-16 characters after trimming whitespace.</param>
    public static unsafe CpuTraceScope TraceScope(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (name.Contains('\0'))
        {
            throw new ArgumentException("Trace names cannot contain NUL.", nameof(name));
        }

        byte[] utf8 = Encoding.UTF8.GetBytes(name + "\0");
        fixed (byte* pointer = utf8)
        {
            long token = Bind_Profiler.CallBeginCpuTraceScope(pointer);
            if (token < 0)
            {
                throw new InvalidOperationException("CPU scopes require the game thread and a nonempty name of at most 1024 UTF-16 characters.");
            }
            return new CpuTraceScope(token);
        }
    }
}

/// <summary>A synchronous game-thread CPU trace scope created by <see cref="Profiler.TraceScope"/>.</summary>
/// <remarks>
/// Dispose in LIFO order on the game thread. Do not copy scopes or keep one across
/// await, thread switches or managed hot reload. Use a using statement.
/// </remarks>
public ref struct CpuTraceScope
{
    private long _token;
    internal CpuTraceScope(long token) => _token = token;

    public void Dispose()
    {
        if (_token < 0)
        {
            return;
        }
        if (Bind_Profiler.CallEndCpuTraceScope(_token) == 0)
        {
            throw new InvalidOperationException("CPU trace scopes must end on the game thread in LIFO order.");
        }
        _token = -1;
    }
}
