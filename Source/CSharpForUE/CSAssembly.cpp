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
	
	Assembly.Handle = UCSManager::Get().GetManagedPluginsCallbacks().LoadPlugin(*AssemblyPath);
	Assembly.Type = GCHandleType::WeakHandle;

	if (!IsAssemblyValid())
	{
		UE_LOG(LogUnrealSharp, Fatal, TEXT("Failed to load: %s"), *AssemblyPath);
		return false;
	}
	
	return true;
}

bool FCSAssembly::Unload() const
{
	return UCSManager::Get().GetManagedPluginsCallbacks().UnloadPlugin(*AssemblyPath);
}

bool FCSAssembly::IsAssemblyValid() const
{
	return !Assembly.IsNull();
}




