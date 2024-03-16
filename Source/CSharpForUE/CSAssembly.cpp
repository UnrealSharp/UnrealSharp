#include "CSAssembly.h"
#include "CSharpForUE.h"
#include "Misc/Paths.h"
#include "CSManager.h"

bool FCSAssembly::Load()
{
	if (IsValid())
	{
		UE_LOG(LogUnrealSharp, Display, TEXT("%s is already loaded"), *AssemblyPath);
		return true;
	}
	
	if (!FPaths::FileExists(AssemblyPath))
	{
		UE_LOG(LogUnrealSharp, Display, TEXT("%s doesn't exist"), *AssemblyPath);
		return false;
	}
	
	Assembly.Handle = FCSManager::ManagedPluginsCallbacks.LoadPlugin(*AssemblyPath);
	Assembly.Type = GCHandleType::WeakHandle;

	if (!IsValid())
	{
		UE_LOG(LogUnrealSharp, Fatal, TEXT("Failed to load: %s"), *AssemblyPath);
		return false;
	}
	
	return true;
}

bool FCSAssembly::Unload() const
{
	return FCSManager::ManagedPluginsCallbacks.UnloadPlugin(*AssemblyPath);
}

bool FCSAssembly::IsValid() const
{
	return !Assembly.IsNull();
}

const GCHandleIntPtr& FCSAssembly::GetHandle() const
{
	return Assembly.GetHandle();
}




