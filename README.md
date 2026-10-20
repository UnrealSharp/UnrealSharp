# UnrealSharp

UnrealSharp is a free, open-source plugin for writing Unreal Engine 5 games in C# on top of .NET 10.

[Workflow Showcase](https://www.youtube.com/watch?v=xR7M2XgCuNU)

## Features

- **Unreal Engine API in C#**: Derive from any `UClass`. Implement Actors, ActorComponents, and more in C# with full access to the Unreal Engine API.
- **Generated bindings**: The C# API is automatically generated from all reflected C++ code. This includes the engine, plugins, and your own project, so any new reflected types or members are immediately available for use in C#.
- **Hot reload**: Recompile and reload C# code without restarting the editor.
- **Full .NET ecosystem**: Pull in any NuGet package you need.
- **MIT licensed**

## Games made with UnrealSharp

- [Ballistic Brews (Steam Page)](https://store.steampowered.com/app/4095000/Ballistic_Brews/)
- [Forged: Reconquest (Steam Page)](https://store.steampowered.com/app/3509080/Forged_Reconquest/)
- [Prompt Crisis (Steam Page)](https://store.steampowered.com/app/5203720/Prompt_Crisis/)

Making a game with UnrealSharp? [Open an issue](https://github.com/UnrealSharp/UnrealSharp/issues) or submit a PR to add your project here!

## Sample projects

- [Sample Defense Game](https://github.com/UnrealSharp/UnrealSharp-SampleDefenseGame) built for Mini Jam 174.
- [Slime Guzzler](https://github.com/UnrealSharp/Epic-MegaJam-Project) Epic MegaJam 2025 entry.
- [UnrealSharp-Cropout](https://github.com/UnrealSharp/UnrealSharp-Cropout) Epic's Cropout sample, ported from Blueprints to C#.

## Supported platforms

| Platform | Status |
|---|---|
| Windows | Supported |
| macOS | Supported |
| Linux | Planned |
| iOS | Planned |
| Android | Planned |

## Prerequisites

- Unreal Engine 5.6 – 5.8
- .NET 10.0.5 or newer
- A C++ project (strongly recommended, pure Blueprint projects work, but are harder to provide support for)

## Getting started

Visit the website's [Get Started](https://www.unrealsharp.com/getting-started/quickstart) page!

If you want to contribute to the documentation, check out the [docs repository](https://github.com/UnrealSharp/unrealsharp.github.io)!

## Code example

A networked, interactable resource pickup written entirely in C#:

```csharp
using UnrealSharp;
using UnrealSharp.Attributes;
using UnrealSharp.Engine;
using UnrealSharp.Niagara;

namespace ManagedSharpProject;

public delegate void OnIsPickedUp(bool bIsPickedUp);

[UClass]
public partial class AResourceBase : AActor, IInteractable
{
    public AResourceBase()
    {
        Replicates = true;
        RespawnTime = 500.0f;
    }

    [UProperty(DefaultComponent = true, RootComponent = true)]
    public partial UStaticMeshComponent Mesh { get; set; }

    [UProperty(DefaultComponent = true)]
    public partial UHealthComponent HealthComponent { get; set; }

    [UProperty(PropertyFlags.EditDefaultsOnly)]
    public partial int PickUpAmount { get; set; }

    [UProperty(PropertyFlags.EditDefaultsOnly | PropertyFlags.BlueprintReadOnly)]
    protected partial float RespawnTime { get; set; }

    [UProperty(PropertyFlags.BlueprintReadOnly, ReplicatedUsing = nameof(OnRep_IsPickedUp))]
    protected partial bool bIsPickedUp { get; set; }

    [UProperty(PropertyFlags.EditDefaultsOnly)]
    public partial TSoftObjectPtr<UNiagaraSystem>? PickUpEffect { get; set; }

    [UProperty(PropertyFlags.BlueprintAssignable)]
    public partial TMulticastDelegate<OnIsPickedUp> OnIsPickedUp { get; set; }

    public override void BeginPlay()
    {
        HealthComponent.OnDeath += OnDeath;
        base.BeginPlay();
    }

    [UFunction]
    protected virtual void OnDeath(APlayer player) {}

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

        UExperienceComponent experienceComponent = UExperienceComponent.Get(player.PlayerState);
        experienceComponent.AddExperience(PickUpAmount);

        SystemLibrary.SetTimer(OnRespawned, RespawnTime, false);

        bIsPickedUp = true;
        OnRep_IsPickedUp();
    }

    [UFunction]
    public void OnRespawned()
    {
        bIsPickedUp = false;
        OnRep_IsPickedUp();
    }

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

    // Overridable from Blueprints
    [UFunction(FunctionFlags.BlueprintEvent)]
    public partial void OnIsPickedUpChanged(bool bIsPickedUp);
    public partial void OnIsPickedUpChanged_Implementation(bool bIsPickedUp)
    {
        SetActorHiddenInGame(bIsPickedUp);
    }
}
