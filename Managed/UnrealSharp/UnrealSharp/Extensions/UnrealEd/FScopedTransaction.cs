using UnrealSharp.Interop;

namespace UnrealSharp.UnrealEd;

#if WITH_EDITOR

/// <summary>
/// Managed counterpart of FScopedTransaction: the transaction starts on construction and ends on Dispose.
/// Cancel() rolls it back instead of recording an undo entry.
/// </summary>
public sealed class FScopedTransaction : IDisposable
{
    private IntPtr _handle;

    public FScopedTransaction(string description)
    {
        _handle = Bind_ScopedTransaction.CallCreate(description);
    }

    /// <summary>False when no transaction was started, e.g. while playing in editor or inside another transaction.</summary>
    public bool IsValid => _handle != IntPtr.Zero;

    /// <summary>Rolls back the transaction without leaving an undo entry.</summary>
    public void Cancel()
    {
        if (_handle != IntPtr.Zero)
        {
            Bind_ScopedTransaction.CallCancel(_handle);
        }
    }

    ~FScopedTransaction()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (_handle == IntPtr.Zero)
        {
            return;
        }

        Bind_ScopedTransaction.CallDestroy(_handle);
        _handle = IntPtr.Zero;
        GC.SuppressFinalize(this);
    }
}

#endif
