#pragma once

#include "CSFieldType.h"

struct FCSTypeReferenceReflectionData;
class UCSManagedTypeCompiler;

namespace FCSUtilities
{
	bool ResolveCompilerAndReflectionDataForFieldType(ECSFieldType FieldType, UClass*& OutCompilerClass, TSharedPtr<FCSTypeReferenceReflectionData>& OutReflectionData);
	
	void ParseFunctionFlags(uint32 Flags, TArray<const TCHAR*>& Results);
	void ParsePropertyFlags(EPropertyFlags InFlags, TArray<const TCHAR*>& Results);
	void ParseClassFlags(EClassFlags InFlags, TArray<const TCHAR*>& Results);
};
