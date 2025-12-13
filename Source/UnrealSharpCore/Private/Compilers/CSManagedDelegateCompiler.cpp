#include "Compilers/CSManagedDelegateCompiler.h"
#include "Factories/CSFunctionFactory.h"
#include "CSManagedTypeDefinition.h"
#include "UnrealSharpUtils.h"

UCSManagedDelegateCompiler::UCSManagedDelegateCompiler()
{
	FieldType = UDelegateFunction::StaticClass();
}

void UCSManagedDelegateCompiler::Recompile(UField* TypeToRecompile, const TSharedPtr<FCSManagedTypeDefinition>& ManagedTypeDefinition) const
{
	UDelegateFunction* DelegateSignature = static_cast<UDelegateFunction*>(TypeToRecompile);
	TSharedPtr<FCSFunctionReflectionData> FunctionReflectionData = ManagedTypeDefinition->GetReflectionData<FCSFunctionReflectionData>();
	
	FCSUnrealSharpUtils::PurgeStruct(DelegateSignature);
	DelegateSignature->ParmsSize = 0;
	DelegateSignature->ReturnValueOffset = 0;
	DelegateSignature->NumParms = 0;
	DelegateSignature->FunctionFlags = FunctionReflectionData->FunctionFlags;
	
	FCSFunctionFactory::CreateParameters(DelegateSignature, *FunctionReflectionData);
	DelegateSignature->StaticLink(true);
	
	RegisterFieldToLoader(TypeToRecompile, ENotifyRegistrationType::NRT_Struct);
}
