#include "UnrealSharpUtils.h"

FString FUnrealSharpUtils::GetNamespace(const UObject* Object)
{
	FName PackageName = GetModuleName(Object);
	return GetNamespace(PackageName);
}

FString FUnrealSharpUtils::GetNamespace(const FName PackageName)
{
	return FString::Printf(TEXT("%s.%s"), TEXT("UnrealSharp"), *PackageName.ToString());
}

FName FUnrealSharpUtils::GetModuleName(const UObject* Object)
{
	return FPackageName::GetShortFName(Object->GetPackage()->GetFName());
}
