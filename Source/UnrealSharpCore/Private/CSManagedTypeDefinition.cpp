#include "CSManagedTypeDefinition.h"
#include "CSManager.h"
#include "Compilers/CSManagedTypeCompiler.h"
#include "ReflectionData/CSTypeReferenceReflectionData.h"
#include "Utilities/CSMetaDataUtils.h"
#include "Types/CSClass.h"
#include "Engine/Blueprint.h"
#include "UObject/UObjectHash.h"

FOnManagedTypeStructureChanged FCSManagedTypeDefinitionEvents::OnReflectionDataChanged;

TSharedPtr<FCSManagedTypeDefinition> FCSManagedTypeDefinition::CreateFromReflectionData(const TSharedPtr<FCSTypeReferenceReflectionData>& InReflectionData, UCSManagedAssembly* InOwningAssembly, UCSManagedTypeCompiler* Compiler)
{
	TSharedPtr<FCSManagedTypeDefinition> NewDefinition = MakeShared<FCSManagedTypeDefinition>();
	NewDefinition->OwningAssembly = InOwningAssembly;
	NewDefinition->Compiler = Compiler;
	NewDefinition->SetReflectionData(InReflectionData);
	
	NewDefinition->DefinitionField = TStrongObjectPtr(Compiler->CreateField(NewDefinition));
	NewDefinition->SetDirtyFlags(StructuralChanges);
	
	return NewDefinition;
}

TSharedPtr<FCSManagedTypeDefinition> FCSManagedTypeDefinition::CreateFromNativeField(UField* InField, UCSManagedAssembly* InOwningAssembly)
{
	TSharedPtr<FCSManagedTypeDefinition> NewDefinition = MakeShared<FCSManagedTypeDefinition>();
	NewDefinition->DefinitionField = TStrongObjectPtr(InField);
	NewDefinition->OwningAssembly = InOwningAssembly;
	NewDefinition->TypeGCHandle = InOwningAssembly->FindTypeHandle(FCSFieldName::FromNativeBase(InField));
	
	return NewDefinition;
}

UField* FCSManagedTypeDefinition::GetDefinition()
{
#if WITH_EDITOR
	if (bIsRetired)
	{
		return RetiredDefinitionField.Get();
	}
#endif
	Compile();
	return DefinitionField.Get();
}

void FCSManagedTypeDefinition::Compile()
{
	if (!RequiresCompile() || !GetOwningAssembly()->IsAssemblyLoaded())
	{
		return;
	}
	
	DirtyFlags = None;
	Compiler->StartCompilation(SharedThis(this));
}

#if WITH_EDITOR
TSharedPtr<FGCHandle> FCSManagedTypeDefinition::GetTypeGCHandle()
{
	if (bIsRetired)
	{
		return nullptr;
	}

	if (!TypeGCHandle.IsValid() || TypeGCHandle->IsNull())
	{
		FCSFieldName FieldName = ReflectionData.IsValid() ? ReflectionData->FieldName : FCSFieldName::FromNativeBase(DefinitionField.Get());
		TypeGCHandle = OwningAssembly->FindTypeHandle(FieldName);
	}
	
	return TypeGCHandle;
}

UField* FCSManagedTypeDefinition::Retire()
{
	UField* Field = DefinitionField.Get();
	bIsRetired = true;
	DirtyFlags = None;
	TypeGCHandle.Reset();
	RetiredDefinitionField = Field;

	// Generated fields and their Blueprints can also be rooted by the Editor.
	// Release only this definition's generated objects; instances and user assets
	// continue to keep their referenced types alive through normal GC references.
	auto ReleaseGeneratedRoots = [](UObject* Object)
	{
		Object->RemoveFromRoot();
		ForEachObjectWithOuter(Object, [](UObject* Inner)
		{
			Inner->RemoveFromRoot();
		});
	};
	ReleaseGeneratedRoots(Field);

	UE_LOGFMT(LogUnrealSharp, Warning,
		"Managed type {0} was removed from {1}. Existing references are retained without managed behavior; reparent or remove them before saving.",
		GetFieldName().GetFullName(), OwningAssembly->GetAssemblyFileName());

	if (UClass* Class = Cast<UClass>(Field))
	{
		if (UObject* DefaultObject = Class->GetDefaultObject(false))
		{
			ReleaseGeneratedRoots(DefaultObject);
		}
		TArray<UObject*> Instances;
		GetObjectsOfClass(Class, Instances, true, RF_ClassDefaultObject | RF_ArchetypeObject);
		for (const UObject* Instance : Instances)
		{
			UE_LOGFMT(LogUnrealSharp, Warning, "Object {0} still uses removed type {1}", Instance->GetPathName(), GetFieldName().GetFullName());
		}
		TArray<UClass*> DerivedClasses;
		GetDerivedClasses(Class, DerivedClasses, true);
		for (const UClass* DerivedClass : DerivedClasses)
		{
			UE_LOGFMT(LogUnrealSharp, Warning, "Class {0} still derives from removed type {1}", DerivedClass->GetPathName(), GetFieldName().GetFullName());
		}

		Class->ClassFlags |= CLASS_Deprecated | CLASS_Hidden | CLASS_HideDropDown;
	}

	// Free the original name so reintroducing the type creates a fresh field. Keep the
	// compiled-in package: moving it to /Transient would make it look like a Blueprint type.
	UObject* ObjectToRename = Field;
	if (UCSClass* Class = Cast<UCSClass>(Field))
	{
		if (UBlueprint* Blueprint = Class->GetOwningBlueprint())
		{
			ObjectToRename = Blueprint;
			ReleaseGeneratedRoots(Blueprint);
			if (UClass* SkeletonClass = Blueprint->SkeletonGeneratedClass)
			{
				SkeletonClass->SetFlags(RF_Transient);
				ReleaseGeneratedRoots(SkeletonClass);
				if (UObject* DefaultObject = SkeletonClass->GetDefaultObject(false))
				{
					ReleaseGeneratedRoots(DefaultObject);
				}
				SkeletonClass->ClearFlags(RF_Public | RF_Standalone);
			}
		}
	}

	const FName RetiredName = MakeUniqueObjectName(ObjectToRename->GetOuter(), ObjectToRename->GetClass(),
		*FString::Printf(TEXT("DELETED_%s"), *ObjectToRename->GetName()));
	constexpr ERenameFlags RenameFlags = REN_DoNotDirty | REN_DontCreateRedirectors | REN_NonTransactional;
	if (UEnum* Enum = Cast<UEnum>(Field))
	{
		// UUserDefinedEnum::Rename rebuilds values as ordinal uint8 entries. Retained
		// properties must keep the original values and underlying type instead.
		Enum->UObject::Rename(*RetiredName.ToString(), nullptr, RenameFlags);
		Enum->RemoveNamesFromPrimaryList();
	}
	else
	{
		ObjectToRename->Rename(*RetiredName.ToString(), nullptr, RenameFlags);
	}
	ObjectToRename->SetFlags(RF_Transient);
	ObjectToRename->ClearFlags(RF_Public | RF_Standalone);
	Field->SetFlags(RF_Transient);
	Field->ClearFlags(RF_Public | RF_Standalone);

	// The field also owns this definition through ICSManagedTypeInterface. Removing
	// only the registry entry leaves that cycle rooted by this strong pointer.
	DefinitionField.Reset();
	return Field;
}
#endif

void FCSManagedTypeDefinition::SetTypeGCHandle(uint8* GCHandlePtr)
{
	TypeGCHandle = OwningAssembly->AddTypeHandle(ReflectionData->FieldName, GCHandlePtr);
}

void FCSManagedTypeDefinition::SetDirtyFlags(ECSTypeStructuralFlags InDirtyFlags)
{
#if WITH_EDITOR
	if (bIsRetired)
	{
		return;
	}
#endif
	DirtyFlags = InDirtyFlags;
	
	for (int32 i = ReflectionData->Dependencies.Num() - 1; i >= 0; --i)
	{
		const FCSFieldName& Dependency = ReflectionData->Dependencies[i];
		TSharedPtr<FCSManagedTypeDefinition> ManagedTypeDefinition = OwningAssembly->FindManagedTypeDefinition(Dependency);
		
		if (!ManagedTypeDefinition.IsValid())
		{
			UE_LOGFMT(LogUnrealSharp, Verbose, "Failed to find dependent type {0} for dirty propagation of {1}", *Dependency.GetFullName(), *ReflectionData->FieldName.GetFullName());
			continue;
		}
		
		if (ManagedTypeDefinition->GetDirtyFlags() >= InDirtyFlags)
		{
			continue;
		}

		ManagedTypeDefinition->SetDirtyFlags(InDirtyFlags);
	}
}
