#pragma once

#include "CoreMinimal.h"
#include "UnrealSharpUtilities.h"
#include "Kismet/BlueprintFunctionLibrary.h"

#define UE_VERSION_VAL(Major, Minor) ((Major) * 10000 + (Minor))
#define UE_CURRENT_VERSION UE_VERSION_VAL(ENGINE_MAJOR_VERSION, ENGINE_MINOR_VERSION)
#define UE_VERSION_BEFORE(Major, Minor) (UE_CURRENT_VERSION < UE_VERSION_VAL(Major, Minor))
#define UE_VERSION_SINCE(Major, Minor)  (UE_CURRENT_VERSION >= UE_VERSION_VAL(Major, Minor))
#define UE_VERSION_EQUAL(Major, Minor)  (UE_CURRENT_VERSION == UE_VERSION_VAL(Major, Minor))

#if ENGINE_MAJOR_VERSION > 5 || ENGINE_MINOR_VERSION >= 6
#define US_LOGFMT(Category, Verbosity, Fmt, ...) \
        UE_LOGFMT(Category, Verbosity, Fmt, ##__VA_ARGS__)
#else
#define US_LOGFMT(Category, Verbosity, Fmt, ...) \
        UE_LOG(Category, Verbosity, TEXT("%s"), *FString::Printf(TEXT(Fmt), ##__VA_ARGS__))
#endif

namespace FCSUnrealSharpUtils
{
	UNREALSHARPUTILITIES_API FName GetNamespace(const UObject* Object);
	UNREALSHARPUTILITIES_API FName GetNamespace(FName PackageName);
	UNREALSHARPUTILITIES_API FName GetNativeFullName(const UField* Object);

	UNREALSHARPUTILITIES_API void PurgeMetaData(const UObject* Object);
	
	UNREALSHARPUTILITIES_API FName GetModuleName(const UObject* Object);

	UNREALSHARPUTILITIES_API bool IsStandalonePIE();

	UNREALSHARPUTILITIES_API void PurgeStruct(UStruct* Struct);

	UNREALSHARPUTILITIES_API inline FGuid ConstructGUIDFromString(const FString& Name)
	{
		if (Name.IsEmpty())
		{
			US_LOGFMT(LogUnrealSharpUtilities, Warning, "Tried to construct a GUID from an empty string. Returning an invalid GUID.");
			return FGuid();
		}
		
		const uint32 BufferLength = Name.Len() * sizeof(Name[0]); 
		uint32 HashBuffer[5]; 
		FSHA1::HashBuffer(*Name, BufferLength, reinterpret_cast<uint8*>(HashBuffer)); 
		return FGuid(HashBuffer[1], HashBuffer[2], HashBuffer[3], HashBuffer[4]);
	}
	
	UNREALSHARPUTILITIES_API inline FGuid ConstructGUIDFromName(const FName& Name)
	{
		return ConstructGUIDFromString(Name.ToString());
	}
	
	UNREALSHARPUTILITIES_API inline bool IsEngineStartingUp() { return GIsInitialLoad; }
	
	UNREALSHARPUTILITIES_API FString MakeQuotedPath(const FString& Path);

	template<typename T>
	static void GetAllCDOsOfClass(TArray<T*>& OutObjects)
	{
		for (TObjectIterator<UClass> It; It; ++It)
		{
			UClass* ClassObject = *It;
		
			if (!ClassObject->IsChildOf(T::StaticClass()) || ClassObject->HasAnyClassFlags(CLASS_Abstract))
			{
				continue;
			}

			T* CDO = ClassObject->GetDefaultObject<T>();
			OutObjects.Add(CDO);
		}
	}
};
