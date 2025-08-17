#include "CSGeneratedDelegateBuilder.h"
#include "CSMetaDataUtils.h"
#include "MetaData/CSDelegateMetaData.h"
#include "TypeGenerator/Factories/CSFunctionFactory.h"
#include "TypeInfo/CSManagedTypeInfo.h"
#include "UnrealSharpUtilities/UnrealSharpUtils.h"

void UCSGeneratedDelegateBuilder::RebuildType(UField* TypeToBuild, const TSharedPtr<FCSManagedTypeInfo>& ManagedTypeInfo) const
{
	UDelegateFunction* Field = CastChecked<UDelegateFunction>(TypeToBuild);
	TSharedPtr<FCSDelegateMetaData> TypeMetaData = ManagedTypeInfo->GetTypeMetaData<FCSDelegateMetaData>();
	
	FCSUnrealSharpUtils::PurgeStruct(Field);
	Field->ParmsSize = 0;
	Field->ReturnValueOffset = 0;
	Field->NumParms = 0;
	
	const FCSFunctionMetaData& SignatureFunctionMetaData = TypeMetaData->SignatureFunction;
	Field->FunctionFlags = SignatureFunctionMetaData.FunctionFlags;
	FCSFunctionFactory::CreateParameters(Field, SignatureFunctionMetaData);
	Field->StaticLink(true);
    FCSMetaDataUtils::ApplyMetaData(TypeMetaData->MetaData, Field);
	
	RegisterFieldToLoader(TypeToBuild, ENotifyRegistrationType::NRT_Struct);
}

UClass* UCSGeneratedDelegateBuilder::GetFieldType() const
{
	return UDelegateFunction::StaticClass();
}
