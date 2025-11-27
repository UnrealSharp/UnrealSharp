#pragma once

#include "CSFieldType.h"

class UCSManagedAssembly;
struct FCSTypeReferenceReflectionData;
class UCSManagedTypeCompiler;

namespace FCSUtilities
{
	UNREALSHARPCORE_API bool ResolveCompilerAndReflectionDataForFieldType(ECSFieldType FieldType, UClass*& OutCompilerClass, TSharedPtr<FCSTypeReferenceReflectionData>& OutReflectionData);
	
	UNREALSHARPCORE_API void ParseFunctionFlags(uint32 Flags, TArray<const TCHAR*>& Results);
	UNREALSHARPCORE_API void ParsePropertyFlags(EPropertyFlags InFlags, TArray<const TCHAR*>& Results);
	UNREALSHARPCORE_API void ParseClassFlags(EClassFlags InFlags, TArray<const TCHAR*>& Results);
	
#if WITH_EDITOR
	UNREALSHARPCORE_API void SortAssembliesByDependencyOrder(const TArray<UCSManagedAssembly*>& InputAssemblies, TArray<UCSManagedAssembly*>& OutSortedAssemblies);
	UNREALSHARPCORE_API void GetReferencedAssemblies(UCSManagedAssembly* Assembly, TArray<UCSManagedAssembly*>& OutReferencedAssemblies);
#endif
};
