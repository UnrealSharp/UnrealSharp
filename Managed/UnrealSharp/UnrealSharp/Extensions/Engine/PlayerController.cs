using UnrealSharp.UnrealSharpCore;

namespace UnrealSharp.Engine;

public partial class APlayerController
{
	/// <summary>
	/// Returns the ULocalPlayer for this controller if it exists, or null otherwise
	/// </summary>
	public ULocalPlayer LocalPlayer => UCSPlayerControllerExtensions.GetLocalPlayer(this);
	
	/// <summary>
	/// Returns the ULocalPlayer for this controller cast to the specified type T
	/// </summary>
	/// <typeparam name="T"> The type to cast the ULocalPlayer to </typeparam>
	/// <returns> The ULocalPlayer cast to type T </returns>
	public T LocalPlayerAs<T>() where T : ULocalPlayer => (T) LocalPlayer;
}