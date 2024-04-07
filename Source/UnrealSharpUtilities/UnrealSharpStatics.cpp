#include "UnrealSharpStatics.h"

FString UUnrealSharpStatics::GetNamespace(const UObject* Object)
{
	FName PackageName = GetModuleName(Object);
	return GetNamespace(PackageName);
}

FString UUnrealSharpStatics::GetNamespace(const FName PackageName)
{
	return FString::Printf(TEXT("%s.%s"), UNREAL_SHARP_NAMESPACE, *PackageName.ToString());
}

FName UUnrealSharpStatics::GetModuleName(const UObject* Object)
{
	return FPackageName::GetShortFName(Object->GetPackage()->GetFName());
}
