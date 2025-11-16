#include "TypeInfo/CSManagedTypeInfo.h"
#include "CSManager.h"
#include "Builders/CSGeneratedTypeBuilder.h"
#include "MetaData/CSTypeReferenceMetaData.h"
#include "Utilities/CSMetaDataUtils.h"

FOnStructureChanged FCSManagedTypeInfoDelegates::OnStructureChangedDelegate;

TSharedPtr<FCSManagedTypeInfo> FCSManagedTypeInfo::CreateManaged(TSharedPtr<FCSTypeReferenceMetaData> MetaData, UCSAssembly* InOwningAssembly, UCSGeneratedTypeBuilder* Builder)
{
	TSharedPtr<FCSManagedTypeInfo> NewTypeInfo = MakeShared<FCSManagedTypeInfo>();
	NewTypeInfo->OwningAssembly = InOwningAssembly;
	NewTypeInfo->SetMetaData(MetaData);
	NewTypeInfo->Builder = Builder;
	NewTypeInfo->Field = TStrongObjectPtr(Builder->CreateField(NewTypeInfo));
	NewTypeInfo->MarkAsStructurallyModified();
	
	return NewTypeInfo;
}

TSharedPtr<FCSManagedTypeInfo> FCSManagedTypeInfo::CreateNative(UField* InField, UCSAssembly* InOwningAssembly)
{
	TSharedPtr<FCSManagedTypeInfo> NewTypeInfo = MakeShared<FCSManagedTypeInfo>();
	NewTypeInfo->Field = TStrongObjectPtr(InField);
	NewTypeInfo->OwningAssembly = InOwningAssembly;
	NewTypeInfo->TypeHandle = InOwningAssembly->TryFindTypeHandle(FCSFieldName(InField));
	NewTypeInfo->bHasChangedStructure = false;
	
	return NewTypeInfo;
}

#if WITH_EDITOR
TSharedPtr<FGCHandle> FCSManagedTypeInfo::GetTypeHandle()
{
	if (!TypeHandle.IsValid() || TypeHandle->IsNull())
	{
		FCSFieldName FieldName = MetaData.IsValid() ? MetaData->FieldName : FCSFieldName(Field.Get());
		TypeHandle = OwningAssembly->TryFindTypeHandle(FieldName);
	}
	
	return TypeHandle;
}
#endif

void FCSManagedTypeInfo::SetTypeHandle(uint8* TypeHandlePtr)
{
	TypeHandle = OwningAssembly->RegisterTypeHandle(MetaData->FieldName, TypeHandlePtr);
}

void FCSManagedTypeInfo::MarkAsStructurallyModified()
{
	if (bHasChangedStructure)
	{
		return;
	}

	// Notify dependent types to rebuild as well. These are spawned by source generators and depend on this type's structure.
	// Such as the async wrapper classes.
	for (int32 i = MetaData->SourceGeneratorDependencies.Num() - 1; i >= 0; --i)
	{
		const FCSFieldName& SourceGeneratorDependency = MetaData->SourceGeneratorDependencies[i];
		TSharedPtr<FCSManagedTypeInfo> DependentTypeInfo = OwningAssembly->FindTypeInfo(SourceGeneratorDependency);
		
		if (!DependentTypeInfo.IsValid())
		{
			continue;
		}

		DependentTypeInfo->MarkAsStructurallyModified();
	}
	
	bHasChangedStructure = true;
	FCSManagedTypeInfoDelegates::OnStructureChangedDelegate.Broadcast(SharedThis(this));
}

UField* FCSManagedTypeInfo::GetOrBuildField()
{
	UField* FieldPtr = Field.Get();
	
	if (bHasChangedStructure)
	{
		bHasChangedStructure = false;
		Builder->TriggerRebuild(FieldPtr, SharedThis(this));
	}
	
	return FieldPtr;
}
