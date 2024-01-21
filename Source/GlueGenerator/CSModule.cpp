#include "CSModule.h"
#include "CSScriptBuilder.h"
#include "Misc/Paths.h"

FCSModule::FCSModule(FName InModuleName, const FString& SourceDirectory) : ModuleName(InModuleName)
{
	Namespace = FString::Printf(UNREAL_SHARP_NAMESPACE TEXT(".%s"), *InModuleName.ToString());
	Directory = FPaths::Combine(*SourceDirectory, *InModuleName.ToString());

	IFileManager& FileManager = IFileManager::Get();
	
	if (!FileManager.DirectoryExists(*FPaths::GetPath(Directory)))
	{
		FileManager.MakeDirectory(*FPaths::GetPath(Directory), true);
	}
}
