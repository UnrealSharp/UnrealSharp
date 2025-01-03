using UnrealSharp.Engine;

namespace UnrealSharp.UMG;

public partial class UWidget
{
    /// <summary>
    /// Get the owning player of this widget as a specific type.
    /// </summary>
    /// <returns>The owning player of this widget.</returns>
    public T OwningPlayerAs<T>() where T : APlayerController => (T) OwningPlayer;
    
    /// <summary>
    /// Get the owning local player of this widget as a specific type.
    /// </summary>
    /// <typeparam name="T">The type of the local player.</typeparam>
    /// <returns>The owning local player of this widget.</returns>
    public T OwningLocalPlayerAs<T>() where T : ULocalPlayer => (T) OwningLocalPlayer;
}