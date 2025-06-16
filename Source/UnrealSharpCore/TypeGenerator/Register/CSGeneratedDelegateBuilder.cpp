#include "CSGeneratedDelegateBuilder.h"

#include "TypeGenerator/Factories/CSFunctionFactory.h"
#include "UnrealSharpUtilities/UnrealSharpUtils.h"

void FCSGeneratedDelegateBuilder::RebuildType()
{
	FUnrealSharpUtils::PurgeStruct(Field);
	Field->ParmsSize = 0;
	Field->ReturnValueOffset = 0;
	Field->NumParms = 0;
	
	const FCSFunctionMetaData& SignatureFunctionMetaData = TypeMetaData->SignatureFunction;
	Field->FunctionFlags = SignatureFunctionMetaData.FunctionFlags;
	FCSFunctionFactory::CreateParameters(Field, SignatureFunctionMetaData);
	Field->StaticLink(true);
	
	RegisterFieldToLoader(ENotifyRegistrationType::NRT_Struct);
}
