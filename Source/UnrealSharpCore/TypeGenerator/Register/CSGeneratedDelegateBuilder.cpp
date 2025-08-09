#include "CSGeneratedDelegateBuilder.h"

#include "TypeGenerator/Factories/CSFunctionFactory.h"
#include "UnrealSharpUtilities/UnrealSharpUtils.h"

void FCSGeneratedDelegateBuilder::RebuildType()
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
