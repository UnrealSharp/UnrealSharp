#include "CSFieldName.h"
#include "UnrealSharpUtilities/UnrealSharpUtils.h"
#include "Utils/CSClassUtilities.h"

FCSFieldName::FCSFieldName(UClass* Class)
{
	const UClass* NativeClass = FCSClassUtilities::GetFirstNativeClass(Class);
	Name = NativeClass->GetFName();
	Namespace = FUnrealSharpUtils::GetNamespace(NativeClass);
}
