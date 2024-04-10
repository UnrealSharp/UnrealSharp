using System.DoubleNumerics;
using System.Runtime.InteropServices;
using UnrealSharp.Engine;

namespace UnrealSharp;

[StructLayout(LayoutKind.Sequential)]
public struct ActorInstanceHandle
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
    
    Vector3 GetActorLocation()
    {
        return Actor.GetActorLocation();
    }
    
    Rotator GetActorRotation()
    {
        return Actor.GetActorRotation();
    }
    
    Vector3 GetActorScale()
    {
        return Actor.GetActorScale3D();
    }
    
    Name GetName()
    {
        return Actor.ObjectName;
    }
}