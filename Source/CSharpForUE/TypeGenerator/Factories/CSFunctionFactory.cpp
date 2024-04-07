#include "CSFunctionFactory.h"
#include "CSPropertyFactory.h"
#include "CSharpForUE/TypeGenerator/CSClass.h"
#include "CSharpForUE/TypeGenerator/Register/CSGeneratedClassBuilder.h"
#include "CSharpForUE/TypeGenerator/Register/CSMetaData.h"

UCSFunction* FCSFunctionFactory::CreateFunction(UClass* Outer, const FName& Name, const FFunctionMetaData& FunctionMetaData, EFunctionFlags FunctionFlags, UStruct* ParentFunction, void* ManagedMethod)
{
	UCSFunction* NewFunction = NewObject<UCSFunction>(Outer, UCSFunction::StaticClass(), Name, RF_Public);
	NewFunction->FunctionFlags = FunctionFlags;
	NewFunction->SetSuperStruct(ParentFunction);

	if (ManagedMethod == nullptr)
	{
		ManagedMethod = FCSGeneratedClassBuilder::TryGetManagedFunction(Outer, Name);
	}
	
	NewFunction->SetManagedMethod(ManagedMethod);
	FinalizeFunctionSetup(Outer, NewFunction);

	FMetaDataHelper::ApplyMetaData(FunctionMetaData.MetaData, NewFunction);
	
	return NewFunction;
}

UCSFunction* FCSFunctionFactory::CreateFunctionFromMetaData(UClass* Outer, const FFunctionMetaData& FunctionMetaData)
{
	UCSFunction* NewFunction = CreateFunction(Outer, FunctionMetaData.Name, FunctionMetaData, FunctionMetaData.FunctionFlags);

	// Check if this function has a return value or is just void, otherwise skip.
	if (FunctionMetaData.ReturnValue.Type != nullptr)
	{
		FCSPropertyFactory::CreateAndAssignProperty(NewFunction, FunctionMetaData.ReturnValue, CPF_Parm | CPF_ReturnParm | CPF_OutParm);
	}

	// Create the function's parameters and assign them.
	// AddCppProperty inserts at the beginning of the property list, so we need to add them backwards to ensure a matching function signature.
	for (int32 i = FunctionMetaData.Parameters.Num(); i-- > 0; )
	{
		FCSPropertyFactory::CreateAndAssignProperty(NewFunction, FunctionMetaData.Parameters[i], CPF_Parm);
	}

	NewFunction->StaticLink(true);
	return NewFunction;
}

UCSFunction* FCSFunctionFactory::CreateOverriddenFunction(UClass* Outer, UFunction* ParentFunction)
{
	const EFunctionFlags FunctionFlags = ParentFunction->FunctionFlags & (FUNC_FuncInherit | FUNC_Public | FUNC_Protected | FUNC_Private | FUNC_BlueprintPure);
	UCSFunction* NewFunction = CreateFunction(Outer, ParentFunction->GetFName(), FFunctionMetaData(), FunctionFlags, ParentFunction);
	
	TArray<FProperty*> FunctionProperties;
	for (TFieldIterator<FProperty> PropIt(ParentFunction); PropIt && PropIt->PropertyFlags & CPF_Parm; ++PropIt)
	{
		FProperty* ClonedParam = CastField<FProperty>(FField::Duplicate(*PropIt, NewFunction, PropIt->GetFName(), RF_AllFlags, EInternalObjectFlags::AllFlags & ~(EInternalObjectFlags::Native)));
		ClonedParam->PropertyFlags |= CPF_BlueprintVisible | CPF_BlueprintReadOnly;
		ClonedParam->Next = nullptr;
		FunctionProperties.Add(ClonedParam);
	}

	// Create the function's parameters and assign them.
	// AddCppProperty inserts at the beginning of the property list, so we need to add them backwards to ensure a matching function signature.
	for (int32 i = FunctionProperties.Num(); i-- > 0;)
	{
		NewFunction->AddCppProperty(FunctionProperties[i]);
	}

#if WITH_EDITOR
	UMetaData::CopyMetadata(ParentFunction, NewFunction);
#endif

	// Override the Blueprint function. But don't let Blueprint display this overridden function.
#if WITH_EDITOR
	NewFunction->SetMetaData("BlueprintInternalUseOnly", TEXT("true"));
#endif
	NewFunction->StaticLink(true);
	
	return NewFunction;
}

void FCSFunctionFactory::FinalizeFunctionSetup(UClass* Outer, UCSFunction* NewFunction)
{
	NewFunction->Next = Outer->Children;
	Outer->Children = NewFunction;
	
	// Mark the function as Native as we want the "UClass::InvokeManagedEvent" to always be called on C# UFunctions.
	NewFunction->FunctionFlags |= FUNC_Native;

	// Bind the new UFunction to call "UClass::InvokeManagedEvent" when invoked.
	{
		Outer->AddNativeFunction(*NewFunction->GetName(), &UCSClass::InvokeManagedMethod);
		NewFunction->Bind();
	}
	
	Outer->AddFunctionToFunctionMap(NewFunction, NewFunction->GetFName());
}

void FCSFunctionFactory::GetOverriddenFunctions(const UClass* Outer, const TSharedRef<FClassMetaData>& ClassMetaData, TArray<UFunction*>& VirtualFunctions)
{
	TMap<FName, UFunction*> NameToFunctionMap;
	TMap<FName, UFunction*> InterfaceFunctionMap;
	
	for (TFieldIterator<UFunction> It(Outer, EFieldIteratorFlags::IncludeSuper); It; ++It)
	{
		UFunction* Function = *It;
		if (Function->HasAnyFunctionFlags(FUNC_BlueprintEvent))
		{
			NameToFunctionMap.Add(Function->GetFName(), Function);
		}
	}
	
	for (const FImplementedInterface& Interface : Outer->Interfaces)
	{
		for (TFieldIterator<UFunction> It(Interface.Class); It; ++It)
		{
			UFunction* InterfaceFunction = *It;
			InterfaceFunctionMap.Add(InterfaceFunction->GetFName(), InterfaceFunction);
		}
	}
	
	for (const FString& VirtualFunction : ClassMetaData->VirtualFunctions)
	{
 		FName FunctionName = *VirtualFunction;
    
		if (UFunction* Function = NameToFunctionMap.FindRef(FunctionName))
		{
			VirtualFunctions.Add(Function);
		}
		else if (UFunction* InterfaceFunction = InterfaceFunctionMap.FindRef(FunctionName))
		{
			VirtualFunctions.Add(InterfaceFunction);
		}
	}
}

void FCSFunctionFactory::GenerateVirtualFunctions(UClass* Outer, const TSharedRef<FClassMetaData>& ClassMetaData)
{
	TArray<UFunction*> VirtualFunctions;
	GetOverriddenFunctions(Outer, ClassMetaData, VirtualFunctions);

	for (UFunction* VirtualFunction : VirtualFunctions)
	{
		CreateOverriddenFunction(Outer, VirtualFunction);
	}
}

void FCSFunctionFactory::GenerateFunctions(UClass* Outer, const TArray<FFunctionMetaData>& Functions)
{
	for (const FFunctionMetaData& FunctionMetaData : Functions)
	{
		CreateFunctionFromMetaData(Outer, FunctionMetaData);
	}
}
