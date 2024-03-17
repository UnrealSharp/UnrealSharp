#include "CSModule.h"
#include "Misc/Paths.h"
#include "UnrealSharpUtilities/UnrealSharpStatics.h"

FCSModule::FCSModule(FName InModuleName, const FString& SourceDirectory) : ModuleName(InModuleName)
{
	Namespace = UUnrealSharpStatics::GetNamespace(InModuleName);
	Namespace.ReplaceCharInline('-', '_');

	Directory = FPaths::Combine(*SourceDirectory, *InModuleName.ToString());

	IFileManager& FileManager = IFileManager::Get();
	
	if (!FileManager.DirectoryExists(*FPaths::GetPath(Directory)))
	{
		FileManager.MakeDirectory(*FPaths::GetPath(Directory), true);
	}
}

FString& FCSModule::CreateCSProjectFileContent()
{
	static FString CSProjectFileContent;
	
	if (!CSProjectFileContent.IsEmpty())
	{
		return CSProjectFileContent;
	}
	
	CSProjectFileContent += TEXT("<Project Sdk=\"Microsoft.NET.Sdk\">\n");
	CSProjectFileContent += TEXT("  <PropertyGroup>\n");
	CSProjectFileContent += TEXT("    <TargetFramework>net8.0</TargetFramework>\n");
	CSProjectFileContent += TEXT("    <ImplicitUsings>enable</ImplicitUsings>\n");
	CSProjectFileContent += TEXT("    <Nullable>enable</Nullable>\n");
	CSProjectFileContent += TEXT("  </PropertyGroup>\n");
	CSProjectFileContent += TEXT("	<ItemGroup>\n");
	CSProjectFileContent += TEXT("		<ProjectReference Include=\"..\\..\\UnrealSharp\\UnrealSharp.csproj\"/>\n");
	CSProjectFileContent += TEXT("  </ItemGroup>\n");
	CSProjectFileContent += TEXT("</Project>\n");

	return CSProjectFileContent;
}
