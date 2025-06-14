#include "CSDelegateBasePropertyGenerator.h"

#include "CSManager.h"
#include "TypeGenerator/Factories/CSFunctionFactory.h"
#include "TypeGenerator/Register/MetaData/CSDelegateMetaData.h"

UDelegateFunction* UCSDelegateBasePropertyGenerator::CreateSignatureFunction(const FCSPropertyMetaData& PropertyMetaData)
{
	TSharedPtr<FCSDelegateMetaData> DelegateMetaData = PropertyMetaData.GetTypeMetaData<FCSDelegateMetaData>();
	const FCSFunctionMetaData& SignatureFunctionMetaData = DelegateMetaData->SignatureFunction;

	UPackage* Package = UCSManager::Get().GetGlobalUnrealSharpPackage();
	UDelegateFunction* SignatureFunction = FindObject<UDelegateFunction>(Package, *SignatureFunctionMetaData.Name.ToString());
	if (IsValid(SignatureFunction))
	{
		FString OldSignatureFunctionName = FString::Printf(TEXT("OLD_%s_%d"), *SignatureFunctionMetaData.Name.ToString(), SignatureFunction->GetUniqueID());
		SignatureFunction->Rename(*OldSignatureFunctionName, nullptr, REN_DontCreateRedirectors | REN_ForceNoResetLoaders);
		SignatureFunction->RemoveFromRoot();
		SignatureFunction->MarkAsGarbage();
	}

	SignatureFunction = NewObject<UDelegateFunction>(Package, UDelegateFunction::StaticClass(), SignatureFunctionMetaData.Name, RF_Public | RF_Standalone | RF_MarkAsRootSet);
	SignatureFunction->FunctionFlags = DelegateMetaData->SignatureFunction.FunctionFlags;
	FCSFunctionFactory::CreateParameters(SignatureFunction, DelegateMetaData->SignatureFunction);

	SignatureFunction->StaticLink(true);
	
	return SignatureFunction;
}
