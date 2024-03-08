#include "CSModule.h"
#include "CSScriptBuilder.h"
#include "Misc/Paths.h"

FCSModule::FCSModule(FName InModuleName, const FString& SourceDirectory) : ModuleName(InModuleName)
{
	Namespace = FString::Printf(UNREAL_SHARP_NAMESPACE TEXT(".%s"), *InModuleName.ToString());
	Namespace.ReplaceCharInline('-', '_');

	Directory = FPaths::Combine(*SourceDirectory, *InModuleName.ToString());

	IFileManager& FileManager = IFileManager::Get();
	
	if (!FileManager.DirectoryExists(*FPaths::GetPath(Directory)))
	{
		FileManager.MakeDirectory(*FPaths::GetPath(Directory), true);
	}
}

void FCSModule::AddReferencedModule(const FName& InModuleName)
{
	if (InModuleName != ModuleName && !ReferencedModules.Contains(InModuleName))
	{
		ReferencedModules.Add(InModuleName);
	}
}

void FCSModule::CreateCSProjectFileContent(const FString& ReferencedModules, FString& CSProjectFileContent)
{
	CSProjectFileContent += TEXT("<Project Sdk=\"Microsoft.NET.Sdk\">\n");
	CSProjectFileContent += TEXT("  <PropertyGroup>\n");
	CSProjectFileContent += TEXT("    <TargetFramework>net8.0</TargetFramework>\n");
	CSProjectFileContent += TEXT("    <ImplicitUsings>enable</ImplicitUsings>\n");
	CSProjectFileContent += TEXT("    <Nullable>enable</Nullable>\n");
	CSProjectFileContent += TEXT("    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>\n");
	CSProjectFileContent += TEXT("  </PropertyGroup>\n");
	CSProjectFileContent += TEXT("	<ItemGroup>\n");
	CSProjectFileContent += TEXT("		<ProjectReference Include=\"..\\..\\UnrealSharp\\UnrealSharp.csproj\"/>\n");
	CSProjectFileContent += TEXT("		<ProjectReference Include=\"..\\..\\UnrealSharp.Core\\UnrealSharp.Core.csproj\"/>\n");
	CSProjectFileContent += TEXT("		<ProjectReference Include=\"..\\..\\UnrealSharp.Engine\\UnrealSharp.Engine.csproj\"/>\n");
	CSProjectFileContent += TEXT("		<ProjectReference Include=\"..\\..\\UnrealSharp.CoreUObject\\UnrealSharp.CoreUObject.csproj\"/>\n");
	
	if (!ReferencedModules.IsEmpty())
	{
		CSProjectFileContent += ReferencedModules;
	}
	
	CSProjectFileContent += TEXT("  </ItemGroup>\n");
	CSProjectFileContent += TEXT("</Project>\n");
}
