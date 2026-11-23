#include "Types/CSScriptStruct.h"

#if WITH_EDITOR
#include "Kismet2/StructureEditorUtils.h"
#include "UObject/FieldIterator.h"
#include "UObject/UObjectIterator.h"
#include "UObject/UnrealType.h"

UClass* UCSScriptStruct::SnapshotEditorDataClass = nullptr;

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

	// Same order as FUserDefinedStructureCompilerUtils::CompileStruct: once the snapshot exists, notify the editor while
	// the old layout is still linked. Data tables save their rows, and listeners such as StructUtils, RigVM, Optimus and
	// Python keep or release what they hold. The editor data is dropped when the struct is purged.
	if (SnapshotEditorDataClass)
	{
		EditorData = NewObject<UObject>(this, SnapshotEditorDataClass, NAME_None, RF_Transient);
		FStructureEditorUtils::BroadcastPreChange(this);
		bPendingPostChange = true;
	}

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

void UCSScriptStruct::FinishRecompile()
{
	if (bPendingPostChange)
	{
		bPendingPostChange = false;
		FStructureEditorUtils::BroadcastPostChange(this);
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
