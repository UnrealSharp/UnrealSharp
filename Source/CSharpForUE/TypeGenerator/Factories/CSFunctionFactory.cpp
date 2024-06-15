#include "CSFunctionFactory.h"
#include "CSPropertyFactory.h"
#include "CSharpForUE/TypeGenerator/CSClass.h"
#include "CSharpForUE/TypeGenerator/Register/CSGeneratedClassBuilder.h"
#include "CSharpForUE/TypeGenerator/Register/CSMetaDataUtils.h"

UCSFunction* FCSFunctionFactory::CreateFunction(UClass* Outer, const FName& Name, const FCSFunctionMetaData& FunctionMetaData, EFunctionFlags FunctionFlags, UStruct* ParentFunction)
{
	UCSFunction* NewFunction = NewObject<UCSFunction>(Outer, UCSFunction::StaticClass(), Name, RF_Public);
	NewFunction->FunctionFlags = FunctionMetaData.FunctionFlags | FunctionFlags;
	NewFunction->SetSuperStruct(ParentFunction);
	NewFunction->SetManagedMethod(FCSGeneratedClassBuilder::TryGetManagedFunction(Outer, Name));
	
	FCSMetaDataUtils::ApplyMetaData(FunctionMetaData.MetaData, NewFunction);
	FinalizeFunctionSetup(Outer, NewFunction);
	return NewFunction;
}

FProperty* FCSFunctionFactory::CreateProperty(UCSFunction* Function, const FCSPropertyMetaData& PropertyMetaData)
{
	FProperty* NewParam = FCSPropertyFactory::CreateAndAssignProperty(Function, PropertyMetaData);

	if (!NewParam->HasAnyPropertyFlags(CPF_ZeroConstructor))
	{
		Function->FunctionFlags |= FUNC_HasDefaults;
	}

	return NewParam;
}

UCSFunction* FCSFunctionFactory::CreateFunctionFromMetaData(UClass* Outer, const FCSFunctionMetaData& FunctionMetaData)
{
	UCSFunction* NewFunction = CreateFunction(Outer, FunctionMetaData.Name, FunctionMetaData);

	// Check if this function has a return value or is just void, otherwise skip.
	if (FunctionMetaData.ReturnValue.Type != nullptr)
	{
		CreateProperty(NewFunction, FunctionMetaData.ReturnValue);
	}

	// Create the function's parameters and assign them.
	// AddCppProperty inserts at the beginning of the property list, so we need to add them backwards to ensure a matching function signature.
	for (int32 i = FunctionMetaData.Parameters.Num(); i-- > 0; )
	{
		CreateProperty(NewFunction, FunctionMetaData.Parameters[i]);
	}

	NewFunction->StaticLink(true);
	return NewFunction;
}

UCSFunction* FCSFunctionFactory::CreateOverriddenFunction(UClass* Outer, UFunction* ParentFunction)
{
	#if ENGINE_MINOR_VERSION >= 4
	#define CS_EInternalObjectFlags_AllFlags EInternalObjectFlags_AllFlags
	#else
	#define CS_EInternalObjectFlags_AllFlags EInternalObjectFlags::AllFlags
	#endif
	
	const EFunctionFlags FunctionFlags = ParentFunction->FunctionFlags & (FUNC_FuncInherit | FUNC_Public | FUNC_Protected | FUNC_Private | FUNC_BlueprintPure);
	UCSFunction* NewFunction = CreateFunction(Outer, ParentFunction->GetFName(), FCSFunctionMetaData(), FunctionFlags, ParentFunction);
	
	TArray<FProperty*> FunctionProperties;
	for (TFieldIterator<FProperty> PropIt(ParentFunction); PropIt && PropIt->PropertyFlags & CPF_Parm; ++PropIt)
	{
		FProperty* ClonedParam = CastField<FProperty>(FField::Duplicate(*PropIt, NewFunction, PropIt->GetFName(), RF_AllFlags, CS_EInternalObjectFlags_AllFlags & ~(EInternalObjectFlags::Native)));
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

	// Override the Blueprint function. But don't let Blueprint display this overridden function.
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

void FCSFunctionFactory::GetOverriddenFunctions(const UClass* Outer, const TSharedRef<FCSClassMetaData>& ClassMetaData, TArray<UFunction*>& VirtualFunctions)
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
	
	for (const FName& VirtualFunction : ClassMetaData->VirtualFunctions)
	{
		if (UFunction* Function = NameToFunctionMap.FindRef(VirtualFunction))
		{
			VirtualFunctions.Add(Function);
		}
		else if (UFunction* InterfaceFunction = InterfaceFunctionMap.FindRef(VirtualFunction))
		{
			VirtualFunctions.Add(InterfaceFunction);
		}
	}
}

void FCSFunctionFactory::GenerateVirtualFunctions(UClass* Outer, const TSharedRef<FCSClassMetaData>& ClassMetaData)
{
	TArray<UFunction*> VirtualFunctions;
	GetOverriddenFunctions(Outer, ClassMetaData, VirtualFunctions);

	for (UFunction* VirtualFunction : VirtualFunctions)
	{
		CreateOverriddenFunction(Outer, VirtualFunction);
	}
}

void FCSFunctionFactory::GenerateFunctions(UClass* Outer, const TArray<FCSFunctionMetaData>& Functions)
{
	for (const FCSFunctionMetaData& FunctionMetaData : Functions)
	{
		CreateFunctionFromMetaData(Outer, FunctionMetaData);
	}
}
