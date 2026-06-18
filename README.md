# UnrealSharp

UnrealSharp is a free, open-source plugin for writing Unreal Engine 5 games in C# on top of .NET 10.

[Workflow Showcase](https://www.youtube.com/watch?v=xR7M2XgCuNU)

## Features

- **Unreal Engine API in C#**: Derive from any UClass. Implement Actors, ActorComponents, and more in C# with access to the Unreal Engine API.
- **Generated bindings**: The C# API is automatically generated from all reflected C++ code. This includes the engine, plugins, and your own project, so any new reflected types or members are immediately available for use in C#.
- **Hot reload**: Recompile and reload C# code without restarting the editor.
- **Full .NET ecosystem**: Pull in any NuGet package you need.
- **MIT licensed**

## Supported platforms

| Platform | Status   |
|----------|----------|
| Windows  | Supported |
| macOS    | Supported |
| Linux    | Planned  |
| iOS      | Planned  |
| Android  | Planned  |

## Prerequisites

- Unreal Engine 5.6 - 5.8
- .NET 10.0.5 or newer
- A C++ project (strongly recommended, pure Blueprint projects work but are harder to support)

## Getting started

Visit the website's [Get Started](https://www.unrealsharp.com/getting-started/quickstart) page!

If you want to contribute with documentation, you can contribute to this [repository](https://github.com/UnrealSharp/unrealsharp.github.io)!

## Sample projects

- [Sample Defense Game](https://github.com/UnrealSharp/UnrealSharp-SampleDefenseGame) built for Mini Jam 174.
- [Slime Guzzler](https://github.com/UnrealSharp/Epic-MegaJam-Project) Epic MegaJam 2025 entry.
- [UnrealSharp-Cropout](https://github.com/UnrealSharp/UnrealSharp-Cropout) Epic's Cropout sample, ported from Blueprints to C#.

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
```

## Links

- [Documentation](https://www.unrealsharp.com/) and [FAQ](https://www.unrealsharp.com/faq)
- [Roadmap](https://github.com/orgs/UnrealSharp/projects/3)
- [Discord community](https://discord.gg/HQuJUYFxeV)
- [Documentation repo](https://github.com/UnrealSharp/unrealsharp.github.io)

## Contributing
I accept pull requests and any contributions you make are **greatly appreciated**.

## License

MIT. See [`LICENSE`](LICENSE) for the full text.

## Contact

Discord: **olsson.** (yes, with the dot at the end), or just join the [Discord server](https://discord.gg/HQuJUYFxeV).

## Special Thanks
I'd like to give a huge shoutout to [MonoUE](https://mono-ue.github.io/) (Sadly abandoned :( ) for the great resource for integrating C# into Unreal Engine. Some of the systems are modified versions of their integration, and it's been a real time saver. 
