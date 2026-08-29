#pragma once

#include "CSFieldName.h"
#include "CSFieldType.h"
#include "UnrealSharpCore.h"
#include "Logging/StructuredLog.h"

struct FCSManagedTypeDefinition;
class UCSManagedAssembly;
struct FCSTypeReferenceReflectionData;
class UCSManagedTypeCompiler;

namespace FCSUtilities
{
	UCSManagedTypeCompiler* ResolveCompilerFromFieldType(ECSFieldType FieldType);
	bool ShouldReloadDefinition(const TSharedRef<FCSManagedTypeDefinition>& ManagedTypeDefinition, const TCHAR* NewJsonReflectionData);
	
	UNREALSHARPCORE_API void ParseFunctionFlags(uint32 Flags, TArray<const TCHAR*>& Results);
	UNREALSHARPCORE_API void ParsePropertyFlags(EPropertyFlags InFlags, TArray<const TCHAR*>& Results);
	UNREALSHARPCORE_API void ParseClassFlags(EClassFlags InFlags, TArray<const TCHAR*>& Results);
	
	template<typename T = UField>
	T* FindField(const FCSFieldName& FieldName)
	{
		TRACE_CPUPROFILER_EVENT_SCOPE(FCSUtilities::FindField);

#if WITH_EDITOR
		if (!FieldName.IsValid())
		{
			UE_LOGFMT(LogUnrealSharp, Warning, "Invalid field name: {0}", *FieldName.GetName());
			return nullptr;
		}
#endif

		UPackage* Package = FieldName.GetPackage();
		
#if WITH_EDITOR
		if (!IsValid(Package))
		{
			UE_LOGFMT(LogUnrealSharp, Warning, "Failed to find package for field: {0}", *FieldName.GetName());
			return nullptr;
		}
#endif

		if (T* FoundField = FindObjectFast<T>(Package, *FieldName.GetEngineName()))
		{
			return FoundField;
		}
		
		// Enums are found with their prefix. 
		return FindObjectFast<T>(Package, *FieldName.GetName());
	}
};
