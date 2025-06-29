#include "CSFunctionFactory.h"
#include "CSPropertyFactory.h"
#include "UnrealSharpCore.h"
#include "UnrealSharpCore/TypeGenerator/Register/CSGeneratedClassBuilder.h"
#include "UnrealSharpCore/TypeGenerator/Register/CSMetaDataUtils.h"
#include "TypeGenerator/Functions/CSFunction_Params.h"

UCSFunctionBase* FCSFunctionFactory::CreateFunction(UClass* Outer, const FName& Name, const FCSFunctionMetaData& FunctionMetaData, EFunctionFlags FunctionFlags, UStruct* ParentFunction)
{
	UCSFunctionBase* NewFunction = NewObject<UCSFunctionBase>(Outer, UCSFunctionBase::StaticClass(), Name, RF_Public);
	NewFunction->FunctionFlags = FunctionMetaData.FunctionFlags | FunctionFlags;

	if (NewFunction->HasAllFunctionFlags(FUNC_BlueprintPure))
	{
		NewFunction->FunctionFlags |= FUNC_BlueprintCallable;
	}
	
	NewFunction->SetSuperStruct(ParentFunction);
	
	if (!NewFunction->TryUpdateMethodHandle())
	{
		// If we can't find the method handle, we can't create the function. This is a fatal error.
		return nullptr;
	}
	
	FCSMetaDataUtils::ApplyMetaData(FunctionMetaData.MetaData, NewFunction);
	return NewFunction;
}

FProperty* FCSFunctionFactory::CreateParameter(UFunction* Function, const FCSPropertyMetaData& PropertyMetaData)
{
	FProperty* NewParam = FCSPropertyFactory::CreateAndAssignProperty(Function, PropertyMetaData);

	if (UBlueprintGeneratedClass* Class = Cast<UBlueprintGeneratedClass>(Function->GetOuter()))
	{
		FCSPropertyFactory::TryAddPropertyAsFieldNotify(PropertyMetaData, Class);
	}

	if (!NewParam->HasAnyPropertyFlags(CPF_ZeroConstructor))
	{
		Function->FunctionFlags |= FUNC_HasDefaults;
	}

	return NewParam;
}

void FCSFunctionFactory::CreateParameters(UFunction* Function, const FCSFunctionMetaData& FunctionMetaData)
{
	// Check if this function has a return value or is just void, otherwise skip.
	if (FunctionMetaData.ReturnValue.Type != nullptr)
	{
		CreateParameter(Function, FunctionMetaData.ReturnValue);
	}

	// Create the function's parameters and assign them.
	// AddCppProperty inserts at the beginning of the property list, so we need to add them backwards to ensure a matching function signature.
	for (int32 i = FunctionMetaData.Parameters.Num(); i-- > 0; )
	{
		CreateParameter(Function, FunctionMetaData.Parameters[i]);
	}
}

UCSFunctionBase* FCSFunctionFactory::CreateFunctionFromMetaData(UClass* Outer, const FCSFunctionMetaData& FunctionMetaData)
{
	UCSFunctionBase* NewFunction = CreateFunction(Outer, FunctionMetaData.Name, FunctionMetaData);

	if (!NewFunction)
	{
		return nullptr;
	}

	CreateParameters(NewFunction, FunctionMetaData);
	FinalizeFunctionSetup(Outer, NewFunction);
	return NewFunction;
}

UCSFunctionBase* FCSFunctionFactory::CreateOverriddenFunction(UClass* Outer, UFunction* ParentFunction)
{
	const EFunctionFlags FunctionFlags = ParentFunction->FunctionFlags & (FUNC_FuncInherit | FUNC_Public | FUNC_Protected | FUNC_Private | FUNC_BlueprintPure | FUNC_HasOutParms);
	UCSFunctionBase* NewFunction = CreateFunction(Outer, ParentFunction->GetFName(), FCSFunctionMetaData(), FunctionFlags, ParentFunction);
	
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

#if ENGINE_MAJOR_VERSION * 100 + ENGINE_MINOR_VERSION < 506
	UMetaData::CopyMetadata(ParentFunction, NewFunction);
#else
	FMetaData::CopyMetadata(ParentFunction, NewFunction);
#endif

	// Override the Blueprint function. But don't let Blueprint display this overridden function.
	NewFunction->SetMetaData("BlueprintInternalUseOnly", TEXT("true"));
#endif
	
	FinalizeFunctionSetup(Outer, NewFunction);
	return NewFunction;
}

void FCSFunctionFactory::FinalizeFunctionSetup(UClass* Outer, UCSFunctionBase* Function)
{
	Function->Next = Outer->Children;
	Outer->Children = Function;
	
	// Mark the function as Native as we want the "UClass::InvokeManagedMethod" to always be called on C# UFunctions.
	Function->FunctionFlags |= FUNC_Native;
	Function->StaticLink(true);
	
	if (Function->NumParms == 0)
	{
		Outer->AddNativeFunction(*Function->GetName(), &UCSFunctionBase::InvokeManagedMethod);
	}
	else
	{
		Outer->AddNativeFunction(*Function->GetName(), &UCSFunction_Params::InvokeManagedMethod_Params);
	}
	
	Function->Bind();
	Outer->AddFunctionToFunctionMap(Function, Function->GetFName());
}

void FCSFunctionFactory::GetOverriddenFunctions(const UClass* Outer, const TSharedPtr<const FCSClassMetaData>& ClassMetaData, TArray<UFunction*>& VirtualFunctions)
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

	auto IterateInterfaceFunctions = [&](const UClass* Interface)
	{
		for (TFieldIterator<UFunction> It(Interface); It; ++It)
		{
			UFunction* InterfaceFunction = *It;
			InterfaceFunctionMap.Add(InterfaceFunction->GetFName(), InterfaceFunction);
		}
	};

#if WITH_EDITOR
	// The BP compiler purges the interfaces from the UClass pre-compilation, so we need to get them from the metadata instead.
	for (const FCSTypeReferenceMetaData& InterfaceInfo : ClassMetaData->Interfaces)
	{
		if (UClass* Interface = InterfaceInfo.GetOwningInterface())
		{
			IterateInterfaceFunctions(Interface);
		}
		else
		{
			UE_LOG(LogUnrealSharp, Error, TEXT("Can't find interface: %s"), *InterfaceInfo.FieldName.GetName());
		}
	}
#else
	for (const FImplementedInterface& Interface : Outer->Interfaces)
	{
		IterateInterfaceFunctions(Interface.Class);
	}
#endif
	
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

void FCSFunctionFactory::GenerateVirtualFunctions(UClass* Outer, const TSharedPtr<const FCSClassMetaData>& ClassMetaData)
{
	TArray<UFunction*> VirtualFunctions;
	GetOverriddenFunctions(Outer, ClassMetaData, VirtualFunctions);
	
	for (UFunction* VirtualFunction : VirtualFunctions)
	{
		CreateOverriddenFunction(Outer, VirtualFunction);
	}
}

void FCSFunctionFactory::GenerateFunctions(UClass* Outer, const TArray<FCSFunctionMetaData>& FunctionsMetaData)
{
	for (const FCSFunctionMetaData& FunctionMetaData : FunctionsMetaData)
	{
		CreateFunctionFromMetaData(Outer, FunctionMetaData);
	}
}

void FCSFunctionFactory::AddFunctionToOuter(UClass* Outer, UCSFunctionBase* Function)
{
	Function->Next = Outer->Children;
	Outer->Children = Function;
	Outer->AddFunctionToFunctionMap(Function, Function->GetFName());
}
