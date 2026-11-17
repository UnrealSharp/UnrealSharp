#include "Types/CSScriptStruct.h"

#if WITH_EDITOR
#include "UObject/FieldIterator.h"
#include "UObject/UObjectIterator.h"
#include "UObject/UnrealType.h"

void UCSScriptStruct::PrepareForRecompile()
{
	if (GetStructureSize() == 0)
	{
		return;
	}

	// Existing properties must retain the old layout while their owners are reinstanced.
	const FName DuplicateName = MakeUniqueObjectName(GetTransientPackage(), GetClass(),
		*FString::Printf(TEXT("STRUCT_REINST_%s"), *GetName()));
	TGuardValue DuplicatingGuard(GIsDuplicatingClassForReinstancing, true);
	UCSScriptStruct* Duplicate = CastChecked<UCSScriptStruct>(StaticDuplicateObject(this, GetTransientPackage(), DuplicateName, ~RF_Transactional));
	// Managed definitions are rooted, but their temporary layout snapshots must not be.
	Duplicate->RemoveFromRoot();
	Duplicate->Guid = Guid;
	Duplicate->PrimaryStruct = this;
	Duplicate->Status = UDSS_Duplicate;
	Duplicate->SetFlags(RF_Transient);
	Duplicate->Bind();
	Duplicate->StaticLink(true);
	Duplicate->DefaultStructInstance.Recreate(Duplicate);
	Duplicate->CopyScriptStruct(Duplicate->DefaultStructInstance.GetStructMemory(), GetDefaultInstance());

	TSet<UStruct*> Owners;
	for (TAllFieldsIterator<FStructProperty> It(RF_NoFlags); It; ++It)
	{
		FStructProperty* Property = *It;
		if (Property->Struct == this)
		{
			Property->Struct = Duplicate;
			Owners.Add(Property->GetOwnerStruct());
		}
	}

	for (UStruct* Owner : Owners)
	{
		Owner->CollectBytecodeAndPropertyReferencedObjects();
	}
}
#endif

void UCSScriptStruct::Initialize()
{
#if WITH_EDITOR
	PrimaryStruct = this;
#endif
	
	InitializeStructDefaults();
	UpdateStructFlags();
}

void UCSScriptStruct::InitializeStructDefaults()
{
	int32 Size = FMath::Max(GetStructureSize(), 1);
	StructDefaults = MakeUnique<uint8[]>(Size);
	
	InitializeStructIgnoreDefaults(StructDefaults.Get());
	
	GetManagedCallbacks().InitializeStructure(GetTypeGCHandle()->GetHandle(), StructDefaults.Get());
	
	DefaultStructInstance = FUserStructOnScopeIgnoreDefaults(this, StructDefaults.Get());
	DefaultStructInstance.SetPackage(GetOutermost());
}
