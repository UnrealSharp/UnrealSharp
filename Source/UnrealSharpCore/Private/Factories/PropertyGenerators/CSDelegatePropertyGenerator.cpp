#include "Factories/PropertyGenerators/CSDelegatePropertyGenerator.h"

#include "UnrealSharpCore.h"
#include "Logging/StructuredLog.h"
#include "ReflectionData/CSTemplateType.h"
#include "ReflectionData/CSFieldType.h"

UCSDelegatePropertyGenerator::UCSDelegatePropertyGenerator(FObjectInitializer const& ObjectInitializer) : Super(ObjectInitializer)
{
	TypeToFieldClass =
	{
		{ ECSPropertyType::Delegate, FDelegateProperty::StaticClass() },
		{ ECSPropertyType::MulticastInlineDelegate, FMulticastInlineDelegateProperty::StaticClass() },
		{ ECSPropertyType::DelegateSignature, FDelegateProperty::StaticClass() }
	};

	REGISTER_REFLECTION_DATA(ECSPropertyType::Delegate, FCSTemplateType)
	REGISTER_REFLECTION_DATA(ECSPropertyType::MulticastInlineDelegate, FCSTemplateType)
	REGISTER_REFLECTION_DATA(ECSPropertyType::DelegateSignature, FCSFieldType)
}

FProperty* UCSDelegatePropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyReflectionData& PropertyReflectionData)
{
	FDelegateProperty* DelegateProperty = NewProperty<FDelegateProperty>(Outer, PropertyReflectionData, GetFieldClassForType(PropertyReflectionData));
	TSharedPtr<FCSTemplateType> TemplateType = PropertyReflectionData.GetInnerTypeData<FCSTemplateType>();
	
	const FCSPropertyReflectionData* InnerType = TemplateType->GetTemplateArgument(0);
	TSharedPtr<FCSFieldType> FieldType = InnerType->GetInnerTypeData<FCSFieldType>();
	
	UFunction* GlobalSignature = FieldType->InnerType.ResolveUField<UFunction>();

	if (!IsValid(GlobalSignature))
	{
		UE_LOGFMT(LogUnrealSharp, Error, "Failed to get delegate signature function for delegate property '{0}'", *PropertyReflectionData.FieldName.GetFullName().ToString());
		return nullptr;
	}

	// Clone the delegate signature function as a child of the owning class.
	// Native engine delegates have their signature functions inside the declaring UClass.
	// Global-package-level signatures cause Blueprint latent node expansion to fail
	// (UK2Node_CreateDelegate cannot resolve the delegate type).
	if (UClass* OuterClass = Cast<UClass>(Outer))
	{
		FName LocalSigName = GlobalSignature->GetFName();
		UFunction* LocalSignature = OuterClass->FindFunctionByName(LocalSigName, EIncludeSuperFlag::ExcludeSuper);
		if (!LocalSignature)
		{
			LocalSignature = NewObject<UFunction>(OuterClass, LocalSigName, RF_Public);
			LocalSignature->FunctionFlags = GlobalSignature->FunctionFlags;
			LocalSignature->SetSuperStruct(GlobalSignature);
			
			// Copy parameters from the global signature
			for (TFieldIterator<FProperty> ParamIt(GlobalSignature); ParamIt && (ParamIt->PropertyFlags & CPF_Parm); ++ParamIt)
			{
				FProperty* ClonedParam = CastField<FProperty>(FField::Duplicate(*ParamIt, LocalSignature, ParamIt->GetFName(), RF_AllFlags, EInternalObjectFlags::None));
				ClonedParam->Next = nullptr;
				LocalSignature->AddCppProperty(ClonedParam);
			}
			
			LocalSignature->Next = OuterClass->Children;
			OuterClass->Children = LocalSignature;
			OuterClass->AddFunctionToFunctionMap(LocalSignature, LocalSigName);
			LocalSignature->StaticLink(true);
			
			UE_LOG(LogUnrealSharp, Log, TEXT("[DelegateClone] Cloned %s into %s (Params=%d, Flags=0x%llx)"),
				*LocalSigName.ToString(), *OuterClass->GetName(), LocalSignature->NumParms, (uint64)LocalSignature->FunctionFlags);
		}
		DelegateProperty->SignatureFunction = LocalSignature;
	}
	else
	{
		DelegateProperty->SignatureFunction = GlobalSignature;
	}
	
	return DelegateProperty;
}
