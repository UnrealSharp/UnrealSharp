#include "CSFieldName.h"
#include "UnrealSharpUtilities/UnrealSharpUtils.h"

FCSFieldName::FCSFieldName(FName Name, FName Namespace): Name(Name), Namespace(Namespace)
{
}

FCSFieldName::FCSFieldName(const UClass* NativeClass)
{
	Name = NativeClass->GetFName();
	Namespace = FUnrealSharpUtils::GetNamespace(NativeClass);
}
