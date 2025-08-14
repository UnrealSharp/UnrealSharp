#include "CSManagedTypeInfo.h"
#include "CSManager.h"
#include "TypeGenerator/Register/CSBuilderManager.h"
#include "TypeGenerator/Register/CSGeneratedTypeBuilder.h"
#include "TypeGenerator/Register/CSMetaDataUtils.h"
#include "TypeGenerator/Register/MetaData/CSTypeReferenceMetaData.h"

FCSManagedTypeInfo::FCSManagedTypeInfo(const TSharedPtr<FCSTypeReferenceMetaData>& MetaData,
	UCSAssembly* InOwningAssembly, UClass* InClass): Field(nullptr), FieldClass(InClass), OwningAssembly(InOwningAssembly), TypeMetaData(MetaData)
{

}

FCSManagedTypeInfo::FCSManagedTypeInfo(UField* InField, UCSAssembly* InOwningAssembly,
	const TSharedPtr<FGCHandle>& TypeHandle) : FieldClass(nullptr)
{
	Field = InField;
	OwningAssembly = InOwningAssembly;
	ManagedTypeHandle = TypeHandle;
	State = UpToDate;
}

#if WITH_EDITOR
TSharedPtr<FGCHandle> FCSManagedTypeInfo::GetManagedTypeHandle()
{
	if (!ManagedTypeHandle.IsValid() || ManagedTypeHandle->IsNull())
	{
		// Lazy load the type handle in editor. Gets null during hot reload.
		FCSFieldName FieldName = UCSManager::Get().IsManagedType(Field) ? TypeMetaData->FieldName : FCSFieldName(Field);
		ManagedTypeHandle = OwningAssembly->TryFindTypeHandle(FieldName);

		if (!ManagedTypeHandle.IsValid() || ManagedTypeHandle->IsNull())
		{
			UE_LOGFMT(LogUnrealSharp, Error, "Failed to find type handle for class: {0}", *FieldName.GetFullName().ToString());
			return nullptr;
		}
	}
	return ManagedTypeHandle;
}
#endif

UField* FCSManagedTypeInfo::InitializeBuilder()
{
	if (Field && (State == UpToDate || State == CurrentlyBuilding))
	{
		// No need to rebuild or update
		return Field;
	}
	
	UCSTypeBuilderManager* BuilderManager = UCSManager::Get().GetTypeBuilderManager();
	UCSGeneratedTypeBuilder* TypeBuilder = BuilderManager->BorrowTypeBuilder(SharedThis(this));
	
	Field = TypeBuilder->CreateType();
		
	if (State == NeedRebuild)
	{
		State = CurrentlyBuilding;
		TypeBuilder->RebuildType();
		
#if WITH_EDITOR
		FCSMetaDataUtils::ApplyMetaData(TypeMetaData->MetaData, Field);
#endif
	}
#if WITH_EDITOR
	else if (State == NeedUpdate)
	{
		State = CurrentlyBuilding;
		TypeBuilder->UpdateType();
	}
#endif
	
	State = UpToDate;
	return Field;
}
