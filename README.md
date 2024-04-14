# UnrealSharp

## Introduction
UnrealSharp is a plugin for Unreal Engine 5 that allows game developers to use C# in their projects with the power of .NET 8. This plugin bridges the gap between C# and UE5, providing a seamless and efficient workflow for those who prefer C# over C++/Blueprints.

[Workflow Showcase](https://www.youtube.com/watch?v=NdbiysPTztA)

## Features
- **C# Integration**: Write your game logic in C#.
- **Seamless Unreal Engine 5 Compatibility**: Fully integrated with the latest UE5 features and API.
- **Hot reload:** Compile and reload code on the fly without having to restart the engine for changes.
- **Automatic Bindings:** Automatically generates C# API based on what is exposed to reflection.
- **.NET Ecosystem:** Use any NuGet package to extend functionality.

## Prerequisites
- Unreal Engine 5.3.+ (Will support earlier versions in the future)
- .NET 8.0+

## UnrealSharp 0.2 Issues
- Linux/Mac support is not yet implemented.

## Get Started

Visit [Get Started](https://github.com/UnrealSharp/UnrealSharp/wiki/2.-Get-Started).

## Code Example

```c#
using UnrealSharp;
using UnrealSharp.Attributes;
using UnrealSharp.Engine;
using UnrealSharp.Niagara;

namespace ManagedSharpProject;

public partial class OnIsPickedUpDelegate : MulticastDelegate<OnIsPickedUpDelegate.Signature>
{
    public delegate void Signature(bool bIsPickedUp);
}

[UClass]
// Partial classes are only a requirement if you want UnrealSharp to generate helper methods.
// Such as: MyCustomComponent foundComponent = MyCustomComponent.Get(actorReference);
public partial class ResourceBase : Actor, IInteractable
{
    public ResourceBase()
    {
        SetReplicates(true);
        RespawnTime = 500.0f;
    }
    
    // The mesh of the resource
    [UProperty(DefaultComponent = true, RootComponent = true)]
    public StaticMeshComponent Mesh { get; set; }
    
    // The health component of the resource, if it has one
    [UProperty(DefaultComponent = true)]
    public HealthComponent HealthComponent { get; set; }
    
    [UProperty(PropertyFlags.EditDefaultsOnly)]
    public int PickUpAmount { get; set; }
    
    // The time it takes for the resource to respawn
    [UProperty(PropertyFlags.EditDefaultsOnly | PropertyFlags.BlueprintReadOnly)]
    protected float RespawnTime { get; set; }
    
    // Whether the resource has been picked up, is replicated to clients.
    [UProperty(PropertyFlags.BlueprintReadOnly, ReplicatedUsing = nameof(OnRep_IsPickedUp))]
    protected bool bIsPickedUp { get; set; }
    
    // The effect to play when the resource is picked up
    [UProperty(PropertyFlags.EditDefaultsOnly)]
    public NiagaraSystem? PickUpEffect { get; set; }
    
    // The delegate to call when the resource is picked up, broadcasts on clients too.
    [UProperty(PropertyFlags.BlueprintAssignable)]
    public OnIsPickedUpDelegate OnIsPickedUp { get; set; }

    protected override void ReceiveBeginPlay()
    {
        HealthComponent.OnDeath += OnDeath;
        base.ReceiveBeginPlay();
    }

    [UFunction]
    protected virtual void OnDeath(Player player) {}

    // Interface method implementation
    public void OnInteract(Player player)
    {
        GatherResource(player);
    }
    
    [UFunction(FunctionFlags.BlueprintCallable)]
    protected void GatherResource(Player player)
    {
        if (bIsPickedUp)
        {
            return;
        }

        if (!player.Inventory.AddItem(this, PickUpAmount))
        {
            return;
        }

        // Get the ExperienceComponent from the PlayerState using the generated helper methods.
        ExperienceComponent experienceComponent = ExperienceComponent.Get(player.PlayerState);
        experienceComponent.AddExperience(PickUpAmount);
        
        // Respawn the resource after a certain amount of time
        SetTimer(OnRespawned, RespawnTime, false);
        
        bIsPickedUp = true;
        OnRep_IsPickedUp();
    }
    
    [UFunction]
    public void OnRespawned()
    {
        bIsPickedUp = false;
        OnRep_IsPickedUp();
    }
    
    // This is called when the bIsPickedUp property is replicated
    [UFunction]
    public void OnRep_IsPickedUp()
    {
        if (PickUpEffect is not null)
        {
            NiagaraFunctionLibrary.SpawnSystemAtLocation(this, PickUpEffect, GetActorLocation(), GetActorRotation());
        }
        
        OnIsPickedUpChanged(bIsPickedUp);
        OnIsPickedUp.Invoke(bIsPickedUp);
    }
    
    // This can be overridden in blueprints
    [UFunction(FunctionFlags.BlueprintEvent)]
    public void OnIsPickedUpChanged(bool bIsPickedUp)
    {
        SetActorHiddenInGame(bIsPickedUp);
    }
}
```

## Roadmap
Take a look at the roadmap for planned and developed features!

[Roadmap](https://github.com/orgs/UnrealSharp/projects/3)

## Discord Community 
Join the discord community to stay up to date with the recent updates and plugin support!

[Discord community](https://discord.gg/HQuJUYFxeV)

## Contributing
I accept pull requests and any contributions you make are **greatly appreciated**.

## License
Distributed under the MIT License. See `LICENSE` for more information.

## Contact
Discord: **olsson.** (Yes, with a dot at the end.)

Or join the [Discord community](https://discord.gg/HQuJUYFxeV).

## Special Thanks
I'd like to give a huge shoutout to [MonoUE](https://mono-ue.github.io/) (Sadly abandoned :( ) for the great resource for integrating C# into Unreal Engine. Some of the systems are modified versions of their integration, and it's been a real time saver. 

**Thank you to the MonoUE team!**

