#include "Compilers/CSManagedTypeCompiler.h"
#include "CSManagedAssembly.h"
#include "CSManager.h"
#include "Utilities/CSMetaDataUtils.h"
#include "CSManagedTypeDefinition.h"

UField* UCSManagedTypeCompiler::CreateField(const TSharedPtr<FCSManagedTypeDefinition>& ManagedTypeDefinition) const
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSManagedTypeCompiler::CreateField);
	
	if (!ManagedTypeDefinition.IsValid())
	{
		UE_LOGFMT(LogUnrealSharp, Error, "ManagedTypeDefinition is invalid, cannot create type.");
		return nullptr;
	}
	
	UField* ExistingType = ManagedTypeDefinition->GetDefinitionField();
	if (IsValid(ExistingType))
	{
		UE_LOGFMT(LogUnrealSharp, Warning, "Type: {0} already exists, skipping creation.", *ExistingType->GetName());
		return ExistingType;
	}
	
	TSharedPtr<const FCSTypeReferenceReflectionData> ReflectionData = ManagedTypeDefinition->GetReflectionData();
	UPackage* OwningPackage = UCSManager::Get().GetPackage(ReflectionData->FieldName.GetNamespace());
	FString Name = GetFieldName(ReflectionData);

	UField* NewField = NewObject<UField>(OwningPackage, FieldType, *Name, RF_Public);
	
	if (ICSManagedTypeInterface* ManagedTypeInterface = Cast<ICSManagedTypeInterface>(NewField))
	{
		ManagedTypeInterface->SetManagedTypeDefinition(ManagedTypeDefinition);
	}

	UE_LOGFMT(LogUnrealSharp, VeryVerbose, "Created type: {0} in package: {1}", *Name, *OwningPackage->GetName());
	return NewField;
}

void UCSManagedTypeCompiler::RecompileManagedTypeDefinition(const TSharedRef<FCSManagedTypeDefinition>& ManagedTypeDefinition) const
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSManagedTypeCompiler::RecompileManagedTypeDefinition);
	
	UField* TypeToRecompile = ManagedTypeDefinition->GetDefinitionField();
	
	if (!IsValid(TypeToRecompile))
	{
		FName TypeName = ManagedTypeDefinition->GetEngineName();
		UE_LOGFMT(LogUnrealSharp, Fatal, "Type to recompile is invalid. Needs to be created first. Type name: {0}", *TypeName.ToString());
	}
	
	UE_LOGFMT(LogUnrealSharp, VeryVerbose, "Rebuilding type: {0}", *TypeToRecompile->GetName());
	Recompile(TypeToRecompile, ManagedTypeDefinition);
	
	FCSMetaDataUtils::ApplyMetaData(ManagedTypeDefinition->GetReflectionData()->MetaData, TypeToRecompile);
}

FString UCSManagedTypeCompiler::GetFieldName(TSharedPtr<const FCSTypeReferenceReflectionData>& ReflectionData) const
{
	return FCSMetaDataUtils::GetAdjustedFieldName(ReflectionData->FieldName);
}
