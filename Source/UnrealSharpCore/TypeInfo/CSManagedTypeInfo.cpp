#include "CSManagedTypeInfo.h"
#include "CSManager.h"
#include "TypeGenerator/Register/CSGeneratedTypeBuilder.h"
#include "MetaData/CSTypeReferenceMetaData.h"
#include "TypeGenerator/Register/CSMetaDataUtils.h"

FOnStructureChanged FCSManagedTypeInfoDelegates::OnStructureChangedDelegate;

FCSManagedTypeInfo::FCSManagedTypeInfo(UField* NativeField, UCSAssembly* InOwningAssembly) : Builder(nullptr)
{
	Field = TStrongObjectPtr(NativeField);
	OwningAssembly = InOwningAssembly;
	ManagedTypeHandle = OwningAssembly->TryFindTypeHandle(FCSFieldName(NativeField));

	// Native classes never gets rebuilt.
	bHasChangedStructure = false;
}

FCSManagedTypeInfo::FCSManagedTypeInfo(TSharedPtr<FCSTypeReferenceMetaData> MetaData, UCSAssembly* InOwningAssembly,
	UCSGeneratedTypeBuilder* Builder): Field(nullptr), Builder(Builder), OwningAssembly(InOwningAssembly), TypeMetaData(MetaData)
{

}

TSharedPtr<FCSManagedTypeInfo> FCSManagedTypeInfo::Create(TSharedPtr<FCSTypeReferenceMetaData> MetaData, UCSAssembly* InOwningAssembly, UCSGeneratedTypeBuilder* Builder)
{
	TSharedPtr<FCSManagedTypeInfo> NewTypeInfo = MakeShared<FCSManagedTypeInfo>(MetaData, InOwningAssembly, Builder);
	NewTypeInfo->Field = TStrongObjectPtr(Builder->CreateField(NewTypeInfo));
	NewTypeInfo->MarkAsStructurallyModified();
	return NewTypeInfo;
}

void FCSManagedTypeInfo::SetTypeHandle(uint8* ManagedTypeHandlePtr)
{
	ManagedTypeHandle = OwningAssembly->RegisterTypeHandle(TypeMetaData->FieldName, ManagedTypeHandlePtr);
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
