#include "CSManagedTypeInfo.h"
#include "CSManager.h"
#include "Field/FieldSystemNoiseAlgo.h"
#include "TypeGenerator/Register/CSGeneratedTypeBuilder.h"
#include "MetaData/CSTypeReferenceMetaData.h"
#include "TypeGenerator/Register/CSMetaDataUtils.h"

FCSManagedTypeInfo::FCSManagedTypeInfo(UField* NativeField, UCSAssembly* InOwningAssembly)
{
	SetField(NativeField);
	OwningAssembly = InOwningAssembly;
	ManagedTypeHandle = OwningAssembly->TryFindTypeHandle(FCSFieldName(NativeField));

	// Native classes never gets rebuilt.
	bHasChangedStructure = false;
}

TSharedPtr<FCSManagedTypeInfo> FCSManagedTypeInfo::Create(TSharedPtr<FCSTypeReferenceMetaData> MetaData, UCSAssembly* InOwningAssembly, UCSGeneratedTypeBuilder* Builder)
{
	TSharedPtr<FCSManagedTypeInfo> NewTypeInfo = MakeShared<FCSManagedTypeInfo>(MetaData, InOwningAssembly, Builder);
	NewTypeInfo->SetField(Builder->CreateField(NewTypeInfo));
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
	OnStructureChanged.Broadcast(SharedThis(this));

	UClass* NewClass = Cast<UClass>(Field.Get());
	if (!IsValid(NewClass))
	{
		return;
	}
	
	TArray<UClass*> DerivedClasses;
	GetDerivedClasses(NewClass, DerivedClasses, false);

	for (UClass* DerivedClass : DerivedClasses)
	{
		if (!FCSClassUtilities::IsManagedClass(DerivedClass))
		{
			continue;
		}
		
		UCSClass* ManagedClass = static_cast<UCSClass*>(DerivedClass);
		TSharedPtr<FCSManagedTypeInfo> DerivedClassInfo = ManagedClass->GetManagedTypeInfo();
		DerivedClassInfo->MarkAsStructurallyModified();
	}
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
