#include "CSGeneratedDelegateBuilder.h"
#include "Factories/CSFunctionFactory.h"
#include "TypeInfo/CSManagedTypeInfo.h"
#include "UnrealSharpUtilities/UnrealSharpUtils.h"

UCSGeneratedDelegateBuilder::UCSGeneratedDelegateBuilder()
{
	FieldType = UDelegateFunction::StaticClass();
}

void UCSGeneratedDelegateBuilder::RebuildType(UField* TypeToBuild, const TSharedPtr<FCSManagedTypeInfo>& ManagedTypeInfo) const
{
	UDelegateFunction* Field = static_cast<UDelegateFunction*>(TypeToBuild);
	TSharedPtr<FCSClassBaseMetaData> TypeMetaData = ManagedTypeInfo->GetTypeMetaData<FCSClassBaseMetaData>();
	
	FCSUnrealSharpUtils::PurgeStruct(Field);
	Field->ParmsSize = 0;
	Field->ReturnValueOffset = 0;
	Field->NumParms = 0;
	
	const FCSFunctionMetaData& SignatureFunctionMetaData = TypeMetaData->Functions[0];
	Field->FunctionFlags = SignatureFunctionMetaData.Flags;
	
	FCSFunctionFactory::CreateParameters(Field, SignatureFunctionMetaData);
	Field->StaticLink(true);
	
	RegisterFieldToLoader(TypeToBuild, ENotifyRegistrationType::NRT_Struct);
}
