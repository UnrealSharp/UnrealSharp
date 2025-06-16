#include "CSFieldName.h"

#include "UnrealSharpUtilities/UnrealSharpUtils.h"
#include "Utils/CSClassUtilities.h"

FCSFieldName::FCSFieldName(FName Name, FName Namespace): Name(Name), Namespace(Namespace)
{
}

FCSFieldName::FCSFieldName(UClass* Class)
{
	const UClass* NativeClass = FCSClassUtilities::GetFirstNativeClass(Class);
	
	Name = NativeClass->GetFName();
	Namespace = FUnrealSharpUtils::GetNamespace(NativeClass);
}
