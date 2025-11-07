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
	NewTypeInfo->MetaData = MetaData;
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
