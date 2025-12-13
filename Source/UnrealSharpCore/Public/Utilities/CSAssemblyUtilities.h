#pragma once

class UCSManagedAssembly;

namespace FCSAssemblyUtilities
{
#if WITH_EDITOR
	UNREALSHARPCORE_API void SortAssembliesByDependencyOrder(const TArray<UCSManagedAssembly*>& InputAssemblies, TArray<UCSManagedAssembly*>& OutSortedAssemblies);
	UNREALSHARPCORE_API void GetReferencedAssemblies(UCSManagedAssembly* Assembly, TArray<UCSManagedAssembly*>& OutReferencedAssemblies);
#endif
	
	UNREALSHARPCORE_API bool IsGlueAssembly(const UCSManagedAssembly* Assembly);
};
