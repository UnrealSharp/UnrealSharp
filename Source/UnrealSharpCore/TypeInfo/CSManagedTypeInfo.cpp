#include "CSManagedTypeInfo.h"
#include "CSManager.h"
#include "TypeGenerator/Register/CSGeneratedTypeBuilder.h"
#include "MetaData/CSTypeReferenceMetaData.h"
#include "TypeGenerator/Register/CSBuilderManager.h"

FCSManagedTypeInfo::FCSManagedTypeInfo(TSharedPtr<FCSTypeReferenceMetaData> MetaData,
	UCSAssembly* InOwningAssembly): Field(nullptr), OwningAssembly(InOwningAssembly), TypeMetaData(MetaData)
{
	UCSTypeBuilderManager* TypeBuilderManager = UCSManager::Get().GetTypeBuilderManager();
	CachedTypeBuilder = TypeBuilderManager->GetTypeBuilder(GetFieldClass());
}

FCSManagedTypeInfo::FCSManagedTypeInfo(UField* NativeField, UCSAssembly* InOwningAssembly)
{
	Field = NativeField;
	OwningAssembly = InOwningAssembly;
	ManagedTypeHandle = OwningAssembly->TryFindTypeHandle(FCSFieldName(NativeField));

	// Native classes never gets rebuilt.
	StructureState = UpToDate;
}

void FCSManagedTypeInfo::SetTypeHandle(uint8* ManagedTypeHandlePtr)
{
	ManagedTypeHandle = OwningAssembly->RegisterTypeHandle(TypeMetaData->FieldName, ManagedTypeHandlePtr);
}

void FCSManagedTypeInfo::OnStructureChanged()
{
	StructureState = HasChangedStructure;
	OwningAssembly->AddTypeToRebuild(SharedThis(this));
}

UField* FCSManagedTypeInfo::StartBuildingType()
{
	if (StructureState == HasChangedStructure)
	{
		TSharedPtr<FCSManagedTypeInfo> ThisTypeInfo = SharedThis(this);
		
		Field = CachedTypeBuilder->CreateType(ThisTypeInfo);
		StructureState = UpToDate;
		
		CachedTypeBuilder->RebuildType(Field, ThisTypeInfo);
	}
	
	ensureMsgf(IsValid(Field), TEXT("Field is not valid for type: %s. This should never happen."), *GetFieldClass()->GetName());
	return Field;
}
