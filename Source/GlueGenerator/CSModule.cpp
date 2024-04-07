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
