#include "UFunctionExporter.h"
#include "UnrealSharpCore.h"
#include "Utils/CSClassUtilities.h"

uint16 UUFunctionExporter::GetNativeFunctionParamsSize(const UFunction* NativeFunction)
{
	check(NativeFunction);
	return NativeFunction->ParmsSize;
}

UFunction* UUFunctionExporter::CreateNativeFunctionCustomStructSpecialization(UFunction* NativeFunction,
	FProperty** CustomStructParams, UScriptStruct** CustomStructs)
{
	UClass* Outer = NativeFunction->GetOuterUClass();
	UFunction* Specialization = NewObject<UFunction>(Outer, UFunction::StaticClass());
	Specialization->FunctionFlags = NativeFunction->FunctionFlags;
	Specialization->SetSuperStruct(NativeFunction);
	Specialization->SetNativeFunc(NativeFunction->GetNativeFunc());

	TArray<FProperty*> FunctionProperties;
	for (TFieldIterator<FProperty> PropIt(NativeFunction); PropIt && PropIt->PropertyFlags & CPF_Parm; ++PropIt)
	{
		FProperty* Property = *PropIt;
		FProperty* OutProperty;
		if(Property == *CustomStructParams)
		{
			FStructProperty* CustomStructParam = new FStructProperty(Specialization, Property->GetFName(), Property->GetFlags());
			UScriptStruct* Struct = *CustomStructs++;
			CustomStructParam->Struct = Struct;
			EPropertyFlags Flags = Property->GetPropertyFlags() | CPF_BlueprintVisible | CPF_BlueprintReadOnly;
			if (const auto CppStructOps = Struct->GetCppStructOps())
			{
				const auto Capabilities = CppStructOps->GetCapabilities();
				if(Capabilities.HasZeroConstructor)
				{
					Flags |= CPF_ZeroConstructor;
				}
				else
				{
					Flags &= ~(CPF_ZeroConstructor);
				}
				if(Capabilities.IsPlainOldData)
				{
					Flags |= CPF_IsPlainOldData;
				}
				else
				{
					Flags &= ~(CPF_IsPlainOldData);
				}
			}
			else
			{
				Flags &= ~(CPF_ZeroConstructor | CPF_IsPlainOldData);
			}
			CustomStructParam->PropertyFlags = Flags;
			OutProperty = CastField<FProperty>(CustomStructParam);
			CustomStructParams++;
		}
		else
		{
			OutProperty = CastField<FProperty>(FField::Duplicate(Property, Specialization, Property->GetFName(), RF_AllFlags, CS_EInternalObjectFlags_AllFlags & ~EInternalObjectFlags::Native));
			OutProperty->PropertyFlags |= CPF_BlueprintVisible | CPF_BlueprintReadOnly;
			OutProperty->Next = nullptr;
		}
		Specialization->Script.Add(OutProperty->PropertyFlags & CPF_OutParm ? EX_LocalOutVariable : EX_LocalVariable);
		Specialization->Script.Append((uint8*)&OutProperty, sizeof(FProperty*));
		FunctionProperties.Add(OutProperty);
	}

	for(int32 i = FunctionProperties.Num(); i-- > 0;)
	{
		Specialization->AddCppProperty(FunctionProperties[i]);
	}

	Specialization->Next = Outer->Children;
	Outer->Children = Specialization;

	Specialization->StaticLink(true);

	return Specialization;
}

void UUFunctionExporter::InitializeFunctionParams(UFunction* NativeFunction, void* Params)
{
	check(NativeFunction && Params)
	for (TFieldIterator<FProperty> PropIt(NativeFunction); PropIt; ++PropIt)
	{
		PropIt->InitializeValue_InContainer(Params);
	}
}

bool UUFunctionExporter::HasBlueprintEventBeenImplemented(const UFunction* NativeFunction)
{
	if (!IsValid(NativeFunction))
	{
		return false;
	}

	UClass* FunctionOwner = NativeFunction->GetOwnerClass();
	return !FCSClassUtilities::IsNativeClass(FunctionOwner);
}

