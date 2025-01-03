using UnrealSharp.CoreUObject;
using UnrealSharp.UnrealSharpCore;

namespace UnrealSharp.Engine;

public partial class UActorComponent
{
    /// <summary>
    /// Register a SubObject that will get replicated along with the actor component.
    /// The subobject needs to be manually removed from the list before it gets deleted.
    /// </summary>
    /// <param name="subObject">The subobject to replicate. Use UCSReplicatedObject if you don't have a native alternative.</param>
    /// <param name="netCondition">The condition under which the subobject should be replicated.</param>
    public void AddReplicatedSubObject(UObject subObject, ELifetimeCondition netCondition = ELifetimeCondition.COND_None)
    {
        UCSActorComponentExtensions.AddReplicatedSubObject(this, subObject, netCondition);
    }
    
    /// <summary>
    /// Unregister a SubObject to stop replicating its properties to clients.
    /// This does not remove or delete it from connections where it was already replicated.
    /// By default, a replicated subobject gets deleted on clients when the original pointer on the authority gets invalidated.
    /// If you want to immediately remove it from client use the DestroyReplicatedSubObjectOnRemotePeers or TearOffReplicatedSubObject functions instead of this one.
    /// </summary>
    /// <param name="subObject"></param>
    public void RemoveReplicatedSubObject(UObject subObject)
    {
        UCSActorComponentExtensions.RemoveReplicatedSubObject(this, subObject);
    }
    
    /// <summary>
    /// Has the subobject been registered for replication?
    /// </summary>
    /// <param name="subObject">The subobject to check.</param>
    /// <returns>True if the subobject is registered for replication.</returns>
    public bool IsReplicatedSubObjectRegistered(UObject subObject)
    {
        return UCSActorComponentExtensions.IsReplicatedSubObjectRegistered(this, subObject);
    }
}