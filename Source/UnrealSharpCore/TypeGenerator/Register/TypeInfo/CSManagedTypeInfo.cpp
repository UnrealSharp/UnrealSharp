#include "CSManagedTypeInfo.h"
#include "CSManager.h"
#include "TypeGenerator/Register/CSBuilderManager.h"
#include "TypeGenerator/Register/CSGeneratedTypeBuilder.h"
#include "TypeGenerator/Register/MetaData/CSTypeReferenceMetaData.h"

FCSManagedTypeInfo::FCSManagedTypeInfo(const TSharedPtr<FCSTypeReferenceMetaData>& MetaData,
	UCSAssembly* InOwningAssembly, UClass* InTypeClass): Field(nullptr), FieldClass(InTypeClass), OwningAssembly(InOwningAssembly), TypeMetaData(MetaData)
{
	// TODO: Not great, but does the job for now.
	if (FieldClass.Get() != UDelegateFunction::StaticClass())
	{
		ManagedTypeHandle = FindTypeHandle();
	}
}

FCSManagedTypeInfo::FCSManagedTypeInfo(UField* NativeField, UCSAssembly* InOwningAssembly) : FieldClass(nullptr)
{
	Field = TStrongObjectPtr(NativeField);
	OwningAssembly = InOwningAssembly;
	ManagedTypeHandle = FindTypeHandle();
	StructureState = UpToDate;
}

UField* FCSManagedTypeInfo::StartBuildingManagedType()
{
	if (StructureState == HasChangedStructure)
	{
		UCSTypeBuilderManager* BuilderManager = UCSManager::Get().GetTypeBuilderManager();
		TSharedPtr<FCSManagedTypeInfo> ThisTypeInfo = SharedThis(this);
		
		const UCSGeneratedTypeBuilder* TypeBuilder = BuilderManager->BorrowTypeBuilder(ThisTypeInfo);
		
		Field = TStrongObjectPtr(TypeBuilder->CreateType(ThisTypeInfo));
		TypeBuilder->RebuildType(Field.Get(), ThisTypeInfo);
		StructureState = UpToDate;
	}
	
	ensureMsgf(Field.IsValid(), TEXT("Field is not valid for type: %s. This should never happen."), *GetFieldClass()->GetName());
	return Field.Get();
}

TSharedPtr<FGCHandle> FCSManagedTypeInfo::FindTypeHandle() const
{
	TRACE_CPUPROFILER_EVENT_SCOPE(FCSManagedTypeInfo::FindTypeHandle);
	
	FCSFieldName FieldName = IsNativeType() ? FCSFieldName(Field.Get()) : TypeMetaData->FieldName;
	TSharedPtr<FGCHandle> TypeHandle = OwningAssembly->TryFindTypeHandle(FieldName);

	if (!TypeHandle.IsValid() || TypeHandle->IsNull())
	{
		UE_LOGFMT(LogUnrealSharp, Error, "Failed to find type handle for class: {0}", *FieldName.GetFullName().ToString());
		return nullptr;
	}

	return TypeHandle;
}
