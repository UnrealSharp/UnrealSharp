#include "CSAssembly.h"
#include "CSharpForUE.h"
#include "Misc/Paths.h"
#include "CSManager.h"

bool FCSAssembly::Load()
{
	if (IsAssemblyValid())
	{
		UE_LOG(LogUnrealSharp, Display, TEXT("%s is already loaded"), *AssemblyPath);
		return true;
	}
	
	if (!FPaths::FileExists(AssemblyPath))
	{
		UE_LOG(LogUnrealSharp, Display, TEXT("%s doesn't exist"), *AssemblyPath);
		return false;
	}
	
	AssemblyHandle.Handle = FCSManager::ManagedPluginsCallbacks.LoadPlugin(*AssemblyPath);
	AssemblyHandle.Type = GCHandleType::WeakHandle;

	if (!IsAssemblyValid())
	{
		UE_LOG(LogUnrealSharp, Fatal, TEXT("Failed to load: %s"), *AssemblyPath);
		return false;
	}
	
	return true;
}

bool FCSAssembly::Unload() const
{
	return FCSManager::ManagedPluginsCallbacks.UnloadPlugin(AssemblyHandle.Handle);
}

bool FCSAssembly::IsAssemblyValid() const
{
	return !AssemblyHandle.IsNull();
}

GCHandleIntPtr FCSAssembly::GetAssemblyHandle() const
{
	return AssemblyHandle.GetHandle();
}

void FCSAssembly::GetMetaDataPath(FString& OutPath) const
{
	OutPath = FPaths::ChangeExtension(AssemblyPath, TEXT(".json"));
}




