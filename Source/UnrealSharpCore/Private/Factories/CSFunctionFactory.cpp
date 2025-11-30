#include "Factories/CSFunctionFactory.h"
#include "UnrealSharpCore.h"
#include "Compilers/CSManagedClassCompiler.h"
#include "Factories/CSPropertyFactory.h"
#include "Utilities/CSMetaDataUtils.h"
#include "Functions/CSFunction_Params.h"
#include "UnrealSharpUtils.h"

UCSFunctionBase* FCSFunctionFactory::CreateFunction(UClass* Outer, const FName& Name, const FCSFunctionReflectionData& FunctionReflectionData, EFunctionFlags FunctionFlags, UStruct* ParentFunction)
{
	UCSFunctionBase* NewFunction = NewObject<UCSFunctionBase>(Outer, UCSFunctionBase::StaticClass(), Name, RF_Public);
	
	NewFunction->FunctionFlags = FunctionReflectionData.FunctionFlags | FunctionFlags;
	NewFunction->SetSuperStruct(ParentFunction);
	
	FCSMetaDataUtils::ApplyMetaData(FunctionReflectionData.MetaData, NewFunction);
	
	return NewFunction;
}

FProperty* FCSFunctionFactory::CreateParameter(UFunction* Function, const FCSPropertyReflectionData& PropertyReflectionData)
{
	FProperty* NewParam = FCSPropertyFactory::CreateAndAssignProperty(Function, PropertyReflectionData);

	if (UBlueprintGeneratedClass* Class = Cast<UBlueprintGeneratedClass>(Function->GetOuter()))
	{
		FCSPropertyFactory::TryAddPropertyAsFieldNotify(PropertyReflectionData, Class);
	}

	if (!NewParam->HasAnyPropertyFlags(CPF_ZeroConstructor))
	{
		Function->FunctionFlags |= FUNC_HasDefaults;
	}

	return NewParam;
}

void FCSFunctionFactory::CreateParameters(UFunction* Function, const FCSFunctionReflectionData& PropertyReflectionData)
{
	if (const FCSPropertyReflectionData* ReturnValueReflectionData = PropertyReflectionData.TryGetReturnValue())
	{
		CreateParameter(Function, *ReturnValueReflectionData);
	}

	// Create the function's parameters and assign them.
	// AddCppProperty inserts at the beginning of the property list, so we need to add them backwards to ensure a matching function signature.
	for (int32 i = PropertyReflectionData.Properties.Num(); i-- > 0; )
	{
		CreateParameter(Function, PropertyReflectionData.Properties[i]);
	}
}

UCSFunctionBase* FCSFunctionFactory::CreateFunctionFromReflectionData(UClass* Outer, const FCSFunctionReflectionData& FunctionReflectionData)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(FCSFunctionFactory::CreateFunctionFromReflectionData);
	
	UCSFunctionBase* NewFunction = CreateFunction(Outer, FunctionReflectionData.FieldName.GetFName(), FunctionReflectionData);

	if (!NewFunction)
	{
		return nullptr;
	}

	CreateParameters(NewFunction, FunctionReflectionData);
	FinalizeFunctionSetup(Outer, NewFunction);
	return NewFunction;
}

UCSFunctionBase* FCSFunctionFactory::CreateOverriddenFunction(UClass* Outer, UFunction* ParentFunction)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(FCSFunctionFactory::CreateOverriddenFunction);
	
	constexpr EFunctionFlags InheritFlags = FUNC_FuncInherit | FUNC_Public | FUNC_Protected | FUNC_Private | FUNC_BlueprintPure | FUNC_HasOutParms;
	const EFunctionFlags FunctionFlags = ParentFunction->FunctionFlags & InheritFlags;
	
	UCSFunctionBase* NewFunction = CreateFunction(Outer, ParentFunction->GetFName(), FCSFunctionReflectionData(), FunctionFlags, ParentFunction);
	
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

#if UE_VERSION_BEFORE(5, 6)
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
	
	// Mark the function as Native so we can override with InvokeManagedMethod/InvokeManagedMethod_Params and call into C#
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

	if (!Function->UpdateMethodHandle())
	{
		UE_LOG(LogUnrealSharp, Fatal, TEXT("Failed to find managed method for function: %s"), *Function->GetName());
	}
}

void FCSFunctionFactory::GetOverriddenFunctions(const UClass* Outer, const TSharedPtr<const FCSClassReflectionData>& ClassReflectionData, TArray<UFunction*>& VirtualFunctions)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(FCSFunctionFactory::GetOverriddenFunctions);
	
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
	for (const FCSTypeReferenceReflectionData& InterfaceInfo : ClassReflectionData->Interfaces)
	{
		if (UClass* Interface = InterfaceInfo.GetAsInterface())
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
	
	for (FName VirtualFunction : ClassReflectionData->Overrides)
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

void FCSFunctionFactory::GenerateVirtualFunctions(UClass* Outer, const TSharedPtr<const FCSClassReflectionData>& ClassReflectionData)
{
	TArray<UFunction*> VirtualFunctions;
	GetOverriddenFunctions(Outer, ClassReflectionData, VirtualFunctions);
	
	for (UFunction* VirtualFunction : VirtualFunctions)
	{
		CreateOverriddenFunction(Outer, VirtualFunction);
	}
}

void FCSFunctionFactory::GenerateFunctions(UClass* Outer, const TArray<FCSFunctionReflectionData>& FunctionsReflectionData)
{
	for (const FCSFunctionReflectionData& FunctionMetaData : FunctionsReflectionData)
	{
		CreateFunctionFromReflectionData(Outer, FunctionMetaData);
	}
}

void FCSFunctionFactory::AddFunctionToOuter(UClass* Outer, UCSFunctionBase* Function)
{
	Function->Next = Outer->Children;
	Outer->Children = Function;
	Outer->AddFunctionToFunctionMap(Function, Function->GetFName());
}
