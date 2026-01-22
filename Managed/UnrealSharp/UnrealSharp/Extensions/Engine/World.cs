using UnrealSharp.Interop;
using UnrealSharp.UnrealSharpCore;

namespace UnrealSharp.Engine;

public partial class UWorld
{
    /// <summary>
    /// The URL that was used when loading this World.
    /// </summary>
    public FURL URL => UCSWorldExtensions.WorldURL();

    /// <summary>
    /// Get the game mode of this world.
    /// </summary>
    /// <returns>The game mode of this world.</returns>
    public AGameModeBase GameMode => UGameplayStatics.GameMode;
    
    /// <summary>
    /// Get the game mode of this world as a specific type.
    /// </summary>
    /// <typeparam name="T">The type of the game mode.</typeparam>
    /// <returns>The game mode of this world as the specified type.</returns>
    public T GameModeAs<T>() where T : AGameModeBase => (T) GameMode;
    
    /// <summary>
    /// Get the game instance of this world.
    /// </summary>
    /// <returns>The game instance of this world.</returns>
    public UGameInstance GameInstance => UGameplayStatics.GameInstance;
    
    /// <summary>
    /// Get the game instance of this world as a specific type.
    /// </summary>
    /// <typeparam name="T">The type of the game instance.</typeparam>
    /// <returns>The game instance of this world as the specified type.</returns>
    public T GameInstanceAs<T>() where T : UGameInstance => (T) GameInstance;
    
    /// <summary>
    /// Get the game state of this world.
    /// </summary>
    /// <returns>The game state of this world.</returns>
    public AGameStateBase GameState => UGameplayStatics.GameState;
    
    /// <summary>
    /// Get the game state of this world as a specific type.
    /// </summary>
    /// <typeparam name="T">The type of the game state.</typeparam>
    /// <returns>The game state of this world as the specified type.</returns>
    public T GameStateAs<T>() where T : AGameStateBase => (T) GameState;
    
    /// <summary>
    /// Returns the net mode this world is running under
    /// </summary>
    public ENetMode NetMode => (ENetMode)(int)UWorldExporter.CallGetNetMode(NativeObject);
    
    /// <summary>
    /// Jumps the server to new level.  If bAbsolute is true and we are using seemless traveling, we
    /// will do an absolute travel (URL will be flushed).
    /// </summary>
    /// <param name="url">URL the URL that we are traveling to</param>
    /// <param name="bAbsolute">Whether we are using relative or absolute travel</param>
    /// <param name="bShouldSkipGameNotify">Whether to notify the clients/game or not</param>
    public void ServerTravel(string url, bool bAbsolute = false, bool bShouldSkipGameNotify = false)
	{
		UCSWorldExtensions.ServerTravel(url, bAbsolute, bShouldSkipGameNotify);
	}
}