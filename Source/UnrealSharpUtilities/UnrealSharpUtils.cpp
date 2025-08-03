#include "UnrealSharpUtils.h"

#include "UnrealSharpUtilities.h"

FName FCSUnrealSharpUtils::GetNamespace(const UObject* Object)
{
	FName PackageName = GetModuleName(Object);
	return GetNamespace(PackageName);
}

FName FCSUnrealSharpUtils::GetNamespace(const FName PackageName)
{
	return *FString::Printf(TEXT("%s.%s"), TEXT("UnrealSharp"), *PackageName.ToString());
}

FName FCSUnrealSharpUtils::GetNativeFullName(const UField* Object)
{
	FName Namespace = GetNamespace(Object);
	return *FString::Printf(TEXT("%s.%s"), *Namespace.ToString(), *Object->GetName());
}

FName FCSUnrealSharpUtils::GetModuleName(const UObject* Object)
{
	return FPackageName::GetShortFName(Object->GetPackage()->GetFName());
}

bool FCSUnrealSharpUtils::IsStandalonePIE()
{
#if WITH_EDITOR
	return !GIsEditor;
#else
		return false;
#endif
}

void FCSUnrealSharpUtils::PurgeStruct(UStruct* Struct)
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

FGuid FCSUnrealSharpUtils::ConstructGUIDFromName(const FName& Name)
{
	return ConstructGUIDFromString(Name.ToString());
}

FString FCSUnrealSharpUtils::MakeQuotedPath(const FString& Path)
{
	if (Path.IsEmpty())
	{
		return TEXT("");
	}

	if (Path.StartsWith(TEXT("\"")) && Path.EndsWith(TEXT("\"")))
	{
		return Path;
	}

	return FString::Printf(TEXT("\"%s\""), *Path);
}

FGuid FCSUnrealSharpUtils::ConstructGUIDFromString(const FString& Name)
{
	const uint32 BufferLength = Name.Len() * sizeof(Name[0]);
	uint32 HashBuffer[5];
	FSHA1::HashBuffer(*Name, BufferLength, reinterpret_cast<uint8*>(HashBuffer));
	return FGuid(HashBuffer[1], HashBuffer[2], HashBuffer[3], HashBuffer[4]); 
}
