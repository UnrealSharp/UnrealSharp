using System.Runtime.InteropServices;
using UnrealSharp.Attributes;
using UnrealSharp.CoreUObject;

namespace UnrealSharp.Engine;

/*[StructLayout(LayoutKind.Sequential)]
[UStruct(IsBlittable = true)]
public partial struct ActorInstanceHandle
{
    private WeakObject<Actor> _actor;
    private WeakObject<LightWeightInstanceManager> Manager;
    private int _instanceIndex;
    private int _instanceUID;

    public Actor Actor => _actor.Object;
    public int InstanceUID => _instanceIndex;
    public int InstanceIndex => _instanceUID;

    public bool DoesRepresentClass(SubclassOf<Actor> otherClass)
    {
        return otherClass.IsChildOf(_actor.GetType());
    }
    
    public SceneComponent GetRootComponent()
    {
        return Actor.RootComponent;
    }
    
    Vector GetActorLocation()
    {
        return Actor.GetActorLocation();
    }
    
    Rotator GetActorRotation()
    {
        return Actor.GetActorRotation();
    }
    
    Vector GetActorScale()
    {
        return Actor.GetActorScale3D();
    }
    
    Name GetName()
    {
        return Actor.ObjectName;
    }
}*/