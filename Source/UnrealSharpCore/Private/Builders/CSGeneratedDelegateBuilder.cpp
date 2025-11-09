#include "Builders/CSGeneratedDelegateBuilder.h"
#include "Factories/CSFunctionFactory.h"
#include "TypeInfo/CSManagedTypeInfo.h"
#include "UnrealSharpUtils.h"

UCSGeneratedDelegateBuilder::UCSGeneratedDelegateBuilder()
{
	FieldType = UDelegateFunction::StaticClass();
}

void UCSGeneratedDelegateBuilder::RebuildType(UField* TypeToBuild, const TSharedPtr<FCSManagedTypeInfo>& ManagedTypeInfo) const
{
	UDelegateFunction* Field = static_cast<UDelegateFunction*>(TypeToBuild);
	TSharedPtr<FCSFunctionMetaData> TypeMetaData = ManagedTypeInfo->GetMetaData<FCSFunctionMetaData>();
	
	FCSUnrealSharpUtils::PurgeStruct(Field);
	Field->ParmsSize = 0;
	Field->ReturnValueOffset = 0;
	Field->NumParms = 0;
	Field->FunctionFlags = TypeMetaData->FunctionFlags;
	
	FCSFunctionFactory::CreateParameters(Field, *TypeMetaData);
	Field->StaticLink(true);
	
	RegisterFieldToLoader(TypeToBuild, ENotifyRegistrationType::NRT_Struct);
}
