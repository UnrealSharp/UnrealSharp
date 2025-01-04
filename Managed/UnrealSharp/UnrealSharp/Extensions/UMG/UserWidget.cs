using UnrealSharp.Engine;

namespace UnrealSharp.UMG;

public partial class UUserWidget
{
    /// <summary>
    /// Get the owning player controller of this widget as a specific type.
    /// </summary>
    /// <typeparam name="T">The type of the owning player controller.</typeparam>
    /// <returns>The owning player controller of this widget as the specified type.</returns>
    public T OwningPlayerPawnAs<T>() where T : APawn => (T) OwningPlayerPawn;
}