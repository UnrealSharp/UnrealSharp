namespace UnrealSharp
{
    public enum ENetMode : int
    {
        /** Standalone: a game without networking, with one or more local players. Still considered a server because it has all server functionality. */
        Standalone,
        
        /** Dedicated server: server with no local players. */
        DedicatedServer,
       
        /** Listen server: a server that also has a local player who is hosting the game, available to other players on the network. */
        ListenServer,
       
        /**
	     * Network client: client connected to a remote server.
	     * Note that every mode less than this value is a kind of server, so checking NetMode < NM_Client is always some variety of server.
	     */
        Client,

        MAX,
    }
}
