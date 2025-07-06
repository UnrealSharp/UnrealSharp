#include "UnrealSharpUtils.h"

#include "UnrealSharpUtilities.h"

FName FUnrealSharpUtils::GetNamespace(const UObject* Object)
{
	FName PackageName = GetModuleName(Object);
	return GetNamespace(PackageName);
}

FName FUnrealSharpUtils::GetNamespace(const FName PackageName)
{
	return *FString::Printf(TEXT("%s.%s"), TEXT("UnrealSharp"), *PackageName.ToString());
}

FName FUnrealSharpUtils::GetNativeFullName(const UField* Object)
{
	FName Namespace = GetNamespace(Object);
	return *FString::Printf(TEXT("%s.%s"), *Namespace.ToString(), *Object->GetName());
}

FName FUnrealSharpUtils::GetModuleName(const UObject* Object)
{
	return FPackageName::GetShortFName(Object->GetPackage()->GetFName());
}

bool FUnrealSharpUtils::IsStandalonePIE()
{
#if WITH_EDITOR
	return !GIsEditor;
#else
		return false;
#endif
}

void FUnrealSharpUtils::PurgeStruct(UStruct* Struct)
{
	if (!IsValid(Struct))
	{
		UE_LOG(LogUnrealSharpUtilities, Warning, TEXT("Tried to purge an invalid struct: %s"), *GetNameSafe(Struct));
		return;
	}
	
	Struct->PropertyLink = nullptr;
	Struct->DestructorLink = nullptr;
	Struct->ChildProperties = nullptr;
	Struct->Children = nullptr;
	Struct->PropertiesSize = 0;
	Struct->MinAlignment = 0;
	Struct->RefLink = nullptr;
}
