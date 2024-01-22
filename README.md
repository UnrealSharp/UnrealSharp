# UnrealSharp

## Introduction
UnrealSharp is a plugin for Unreal Engine 5 that allows game developers to use C# in their projects with the power of .NET 8. This plugin bridges the gap between C# and UE5, providing a seamless and efficient workflow for those who prefer C# over C++/Blueprints.

[Workflow Showcase](https://www.youtube.com/watch?v=NdbiysPTztA)

## Features
- **C# Integration**: Write your game logic in C#.
- **Seamless Unreal Engine 5 Compatibility**: Fully integrated with the latest UE5 features and API.
- **Hot reload:** Compile and reload code on the fly without having to restart the engine for changes.
- **Automatic Bindings:** Automatically generates C# API based on what is exposed to Blueprint. Which enables marketplace code plugins a seamless integration with UnrealSharp.

## Prerequisites
- Unreal Engine 5+
- Visual Studio / Rider
- .NET 8.0.1

## UnrealSharp 0.1 Issues
- Linux/Mac support is not yet implemented.
- Packaging not yet implemented.
- Multiplayer is almost done, but is not currently not working properly.
- Delegates exposed to Blueprint not yet implemented, can use pure C# delegates though.
- Hot reload is always full reload of the whole assembly. Will be reworked for 0.2 for speed.

## Get Started

Visit [Get Started](https://github.com/UnrealSharp/UnrealSharp/wiki/2.-Get-Started).

## Roadmap
Take a look at the roadmap for planned and developed features!

[Roadmap](https://github.com/orgs/UnrealSharp/projects/3)

## Discord Community 
Join the discord community to stay up to date with the recent updates!

[Discord community](https://discord.gg/HQuJUYFxeV)

## Contributing
I accept pull requests and any contributions you make are **greatly appreciated**.

## License
Distributed under the MIT License. See `LICENSE` for more information.

## Contact
Discord: **olsson.** (Yes, with a dot in the end.)

Or join the [Discord community](https://discord.gg/HQuJUYFxeV).

## Special Thanks
I'd like to give a huge shoutout to [MonoUE](https://mono-ue.github.io/) (Sadly abandoned :( ) for the great resource for integrating C# into Unreal Engine. Some of the systems are modified versions of their integration, and it's been a real time saver. 

**Thank you to the MonoUE team!**

