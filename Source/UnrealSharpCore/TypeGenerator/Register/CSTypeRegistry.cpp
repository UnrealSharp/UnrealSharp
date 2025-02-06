#include "CSTypeRegistry.h"
#include "Misc/FileHelper.h"
#include "Serialization/JsonSerializer.h"
#include "Serialization/JsonReader.h"
#include "TypeInfo/CSClassInfo.h"

TSharedRef<FCSharpClassInfo> FCSTypeRegistry::FindManagedType(UClass* Class)
{
	FString Name = Class->GetName();
	Name.RemoveFromEnd(TEXT("_C"));
	
	TSharedPtr<FCSharpClassInfo> FoundClassInfo = ManagedClasses.FindRef(*Name);

	// Native classes are populated on the go as they are needed for managed code.
	if (!FoundClassInfo.IsValid())
	{
		FoundClassInfo = MakeShared<FCSharpClassInfo>(Class);
		ManagedClasses.Add(Class->GetFName(), FoundClassInfo);
	}
	else
	{
		FoundClassInfo->TryUpdateTypeHandle();
	}
	
	return FoundClassInfo.ToSharedRef();
}

void FCSTypeRegistry::AddPendingClass(FName ParentClass, FCSharpClassInfo* NewClass)
{
	FPendingClasses& PendingClass = PendingClasses.FindOrAdd(ParentClass);
	PendingClass.Classes.Add(NewClass);
}

UClass* FCSTypeRegistry::GetClassFromName(FName Name)
{
	UClass* FoundType;
	TSharedPtr<FCSharpClassInfo> TypeInfo = Get().ManagedClasses.FindRef(Name);
	if (TypeInfo.IsValid())
	{
		FoundType = TypeInfo->InitializeBuilder();
	}
	else
	{
		FoundType = FindFirstObjectSafe<UClass>(*Name.ToString());
	}

	if (!IsValid(FoundType))
	{
		FoundType = GetInterfaceFromName(Name);
	}
	
	return FoundType;
}

UScriptStruct* FCSTypeRegistry::GetStructFromName(FName Name)
{
	UScriptStruct* FoundType;
	TSharedPtr<FCSharpStructInfo> TypeInfo = Get().ManagedStructs.FindRef(Name);
	if (TypeInfo.IsValid())
	{
		FoundType = TypeInfo->InitializeBuilder();
	}
	else
	{
		FoundType = FindFirstObjectSafe<UScriptStruct>(*Name.ToString());
	}
	return FoundType;
}

UEnum* FCSTypeRegistry::GetEnumFromName(FName Name)
{
	UEnum* FoundType;
	TSharedPtr<FCSharpEnumInfo> TypeInfo = Get().ManagedEnums.FindRef(Name);
	if (TypeInfo.IsValid())
	{
		FoundType = TypeInfo->InitializeBuilder();
	}
	else
	{
		FoundType = FindFirstObjectSafe<UEnum>(*Name.ToString());
	}
	
	return FoundType;
}

UClass* FCSTypeRegistry::GetInterfaceFromName(FName Name)
{
	UClass* FoundType;
	TSharedPtr<FCSharpInterfaceInfo> TypeInfo = Get().ManagedInterfaces.FindRef(Name);
	if (TypeInfo.IsValid())
	{
		FoundType = TypeInfo->InitializeBuilder();
	}
	else
	{
		FoundType = FindFirstObjectSafe<UClass>(*Name.ToString());
	}
	
	return FoundType;
}

void FCSTypeRegistry::RegisterClassToFilePath(const UTF16CHAR* ClassName, const UTF16CHAR* FilePath)
{
	ClassToFilePath.Add(FName(ClassName), FilePath);
}

void FCSTypeRegistry::GetClassFilePath(FName ClassName, FString& OutFilePath)
{
	if (FString* FilePath = ClassToFilePath.Find(ClassName))
	{
		OutFilePath = *FilePath;
	}
}

void FCSTypeRegistry::OnModulesChanged(FName InModuleName, EModuleChangeReason InModuleChangeReason)
{
	if (InModuleChangeReason != EModuleChangeReason::ModuleLoaded)
	{
		return;
	}

	int32 NumPendingClasses = PendingClasses.Num();
	for (auto Itr = PendingClasses.CreateIterator(); Itr; ++Itr)
	{
		UClass* Class = GetClassFromName(Itr.Key());
		
		if (!Class)
		{
			// Class still not loaded from this module.
			continue;
		}

		for (FCSharpClassInfo* PendingClass : Itr.Value().Classes)
		{
			PendingClass->InitializeBuilder();
		}

		Itr.RemoveCurrent();
	}

#if WITH_EDITOR
	if (NumPendingClasses != PendingClasses.Num())
	{
		OnPendingClassesProcessed.Broadcast();
	}
#endif
}
