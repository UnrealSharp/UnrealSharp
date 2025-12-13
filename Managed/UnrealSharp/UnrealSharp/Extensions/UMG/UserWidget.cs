using System.Collections.Generic;
using UnrealSharp.Engine;
using UnrealSharp.UnrealSharpCore;

namespace UnrealSharp.UMG;

public partial class UUserWidget
{
    /// <summary>
    /// Get the owning player controller of this widget as a specific type.
    /// </summary>
    /// <typeparam name="T">The type of the owning player controller.</typeparam>
    /// <returns>The owning player controller of this widget as the specified type.</returns>
    public T OwningPlayerPawnAs<T>() where T : APawn => (T) OwningPlayerPawn;

    /// <summary>
    /// Get the owning player state of this widget.
    /// </summary>
    public APlayerState OwningPlayerState => UCSUserWidgetExtensions.GetOwningPlayerState(this);
    
    /// <summary>
    /// Get the owning player state of this widget as a specific type.
    /// </summary>
    public T OwningPlayerStateAs<T>() where T : APlayerState => (T) OwningPlayerState;
    
    /// <summary>
    /// Get the owning local player of this widget
    /// </summary>
    public ULocalPlayer OwningLocalPlayer => UCSUserWidgetExtensions.GetOwningLocalPlayer(this);
    
    /// <summary>
    /// Get the owning local player of this widget as a specific type.
    /// </summary>
    public T OwningLocalPlayerAs<T>() where T : ULocalPlayer => (T) OwningLocalPlayer;
    
    /// <summary>
    /// Get the owning player controller of this widget.
    /// </summary>
    public APlayerController OwningPlayerController => UCSUserWidgetExtensions.GetOwningPlayerController(this);
    
    /// <summary>
    /// Get the owning player controller of this widget as a specific type.
    /// </summary>
    public T OwningPlayerControllerAs<T>() where T : APlayerController => (T) OwningPlayerController;

    /// <summary>
    /// Get all widgets in this widget's tree.
    /// </summary>
    public IList<UWidget> AllWidgets => UCSUserWidgetExtensions.GetAllWidgets(this);

    /// <summary>
    /// Get all widgets of a specific type in this widget's tree.
    /// </summary>
    public IList<T> AllWidgetsAs<T>() where T : UWidget => AllWidgets.OfType<T>().ToList();
}