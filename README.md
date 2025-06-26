# UnrealSharp

## Introduction
UnrealSharp is a plugin for Unreal Engine 5 that allows game developers to use C# in their projects with the power of .NET 9. This plugin bridges the gap between C# and UE5, providing a seamless and efficient workflow for those who prefer C# over C++/Blueprints.

[Workflow Showcase](https://www.youtube.com/watch?v=NdbiysPTztA)

## Features
- **C# Integration**: Write your game logic in C#.
- **Seamless Unreal Engine 5 Compatibility**: Fully integrated with the latest UE5 features and API.
- **Hot reload:** Compile and reload code on the fly without having to restart the engine for changes.
- **Automatic Bindings:** Automatically generates C# API based on what is exposed to reflection.
- **.NET Ecosystem:** Use any NuGet package to extend functionality.

## Sample Projects

[UnrealSharp-Cropout](https://github.com/UnrealSharp/UnrealSharp-Cropout/tree/main), originally created in Blueprints by Epic Games, now converted into C#.

[Sample Defense Game](https://github.com/UnrealSharp/UnrealSharp-SampleDefenseGame), project made for Mini Jam 174.

## Prerequisites
- Unreal Engine 5.3 - 5.6
- .NET 9.0+

## Frequently Asked Questions

[FAQ](https://www.unrealsharp.com/faq)

## Get Started

Visit the website's [Get Started](https://www.unrealsharp.com/getting-started/quickstart) page!

If you want to contribute with documentation, you can contribute to this [repository](https://github.com/UnrealSharp/unrealsharp.github.io)!

## Code Example

```c#
using UnrealSharp;
using UnrealSharp.Attributes;
using UnrealSharp.Engine;
using UnrealSharp.Niagara;

namespace ManagedSharpProject;

public delegate void OnIsPickedUp(bool bIsPickedUp);

[UClass]
// Partial classes are only a requirement if you want UnrealSharp to generate helper methods.
// Such as: MyCustomComponent foundComponent = MyCustomComponent.Get(actorReference);
public partial class AResourceBase : AActor, IInteractable
{
    public AResourceBase()
    {
        SetReplicates(true);
        RespawnTime = 500.0f;
    }
    
    // The mesh of the resource
    [UProperty(DefaultComponent = true, RootComponent = true)]
    public UStaticMeshComponent Mesh { get; set; }
    
    // The health component of the resource, if it has one
    [UProperty(DefaultComponent = true)]
    public UHealthComponent HealthComponent { get; set; }
    
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
    public TSoftObjectPtr<UNiagaraSystem>? PickUpEffect { get; set; }
    
    // The delegate to call when the resource is picked up, broadcasts on clients too.
    [UProperty(PropertyFlags.BlueprintAssignable)]
    public TMulticastDelegate<OnIsPickedUp> OnIsPickedUp { get; set; }

    protected override void BeginPlay()
    {
        HealthComponent.OnDeath += OnDeath;
        base.BeginPlay();
    }

    [UFunction]
    protected virtual void OnDeath(APlayer player) {}

    // Interface method implementation
    public void OnInteract(APlayer player)
    {
        GatherResource(player);
    }
    
    [UFunction(FunctionFlags.BlueprintCallable)]
    protected void GatherResource(APlayer player)
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
        UExperienceComponent experienceComponent = UExperienceComponent.Get(player.PlayerState);
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
            UNiagaraFunctionLibrary.SpawnSystemAtLocation(this, PickUpEffect, GetActorLocation(), GetActorRotation());
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

