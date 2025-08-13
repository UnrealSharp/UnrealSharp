#include "CSGeneratedDelegateBuilder.h"

#include "CSMetaDataUtils.h"
#include "MetaData/CSDelegateMetaData.h"
#include "TypeGenerator/Factories/CSFunctionFactory.h"
#include "TypeInfo/CSManagedTypeInfo.h"
#include "UnrealSharpUtilities/UnrealSharpUtils.h"

DEFINE_BUILDER_TYPE(UCSGeneratedDelegateBuilder, UDelegateFunction, FCSDelegateMetaData)

void UCSGeneratedDelegateBuilder::RebuildType()
{
	FCSUnrealSharpUtils::PurgeStruct(Field);
	Field->ParmsSize = 0;
	Field->ReturnValueOffset = 0;
	Field->NumParms = 0;
	
	const FCSFunctionMetaData& SignatureFunctionMetaData = TypeMetaData->SignatureFunction;
	Field->FunctionFlags = SignatureFunctionMetaData.FunctionFlags;
	FCSFunctionFactory::CreateParameters(Field, SignatureFunctionMetaData);
	Field->StaticLink(true);
    FCSMetaDataUtils::ApplyMetaData(TypeMetaData->MetaData, Field);
	
	RegisterFieldToLoader(ENotifyRegistrationType::NRT_Struct);
}

UClass* UCSGeneratedDelegateBuilder::GetFieldType() const
{
	return UDelegateFunction::StaticClass();
}
