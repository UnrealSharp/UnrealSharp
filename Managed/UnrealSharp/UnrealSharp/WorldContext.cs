using UnrealSharp.Core.Interop;
using UnrealSharp.CoreUObject;

namespace UnrealSharp;

/// <summary>
/// Provides access to the world context active for the current Unreal thread.
/// </summary>
public static class WorldContext
{
    /// <summary>
    /// The currently scoped world context.
    /// </summary>
    public static UObject? Current => Bind_UCSManager.WorldContextObject as UObject;

    /// <summary>
    /// Temporarily uses the world resolved from <paramref name="worldContextObject"/> for generated function calls.
    /// The returned scope must be disposed on the same thread before awaiting.
    /// </summary>
    public static FWorldContextScope Push(UObject worldContextObject)
    {
        ArgumentNullException.ThrowIfNull(worldContextObject);
        if (!worldContextObject.IsValid())
        {
            throw new ArgumentException("World context object is not valid.", nameof(worldContextObject));
        }

        return new FWorldContextScope(worldContextObject);
    }
}

/// <summary>
/// A synchronous scope which restores the previous world context when disposed.
/// </summary>
public ref struct FWorldContextScope
{
    private bool _active;

    internal FWorldContextScope(UObject worldContextObject)
    {
        Bind_UCSManager.CallPushWorldContext(worldContextObject.NativeObject);
        _active = true;
    }

    public void Dispose()
    {
        if (!_active)
        {
            return;
        }

        Bind_UCSManager.CallPopWorldContext();
        _active = false;
    }
}
