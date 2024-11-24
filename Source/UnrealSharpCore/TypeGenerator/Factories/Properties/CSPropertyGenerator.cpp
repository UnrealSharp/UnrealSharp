#include "CSPropertyGenerator.h"

#include "TypeGenerator/Factories/CSPropertyFactory.h"
#include "TypeGenerator/Register/CSTypeRegistry.h"
#include "TypeGenerator/Register/MetaData/CSObjectMetaData.h"
#if WITH_EDITOR
#include "Kismet2/BlueprintEditorUtils.h"
#endif

ECSPropertyType UCSPropertyGenerator::GetPropertyType() const
{
	return ECSPropertyType::Unknown;
}

FFieldClass* UCSPropertyGenerator::GetPropertyClass()
{
	PURE_VIRTUAL();
	return nullptr;
}

FProperty* UCSPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	return NewProperty(Outer, PropertyMetaData);
}

#if WITH_EDITOR
void UCSPropertyGenerator::CreatePropertyEditor(UBlueprint* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	auto PopulateMetaData = [](const TMap<FString, FString>& InMetaData, TArray<struct FBPVariableMetaDataEntry>& OutMetaDataArray)
	{
		TArray<FBPVariableMetaDataEntry> MetaDataArray;
		for (const TTuple<FString, FString>& Pair : InMetaData)
		{
			FBPVariableMetaDataEntry MetaData;
			MetaData.DataKey = FName(Pair.Key);
			MetaData.DataValue = Pair.Value;
			OutMetaDataArray.Add(MetaData);
		}
	};
	
	FBPVariableDescription& VariableDesc = Outer->NewVariables.AddDefaulted_GetRef();
	VariableDesc.VarName = PropertyMetaData.Name;
	VariableDesc.VarGuid = FGuid::NewGuid();
	VariableDesc.RepNotifyFunc = PropertyMetaData.RepNotifyFunctionName;
	VariableDesc.ReplicationCondition = PropertyMetaData.LifetimeCondition;
	VariableDesc.PropertyFlags = PropertyMetaData.PropertyFlags;
	
	CreatePinInfoEditor(PropertyMetaData, VariableDesc.VarType);
	VariableDesc.VarType.PinCategory = GetPinCategory(PropertyMetaData);
	UCSPropertyGenerator* PropertyGenerator = FCSPropertyFactory::FindPropertyGenerator(PropertyMetaData.Type->PropertyType);
	VariableDesc.VarType.PinSubCategoryObject = PropertyGenerator->GetPinSubCategoryObject(Outer, PropertyMetaData);
	
	PopulateMetaData(PropertyMetaData.MetaData, VariableDesc.MetaDataArray);
}

void UCSPropertyGenerator::CreatePinInfoEditor(const FCSPropertyMetaData& PropertyMetaData, FEdGraphPinType& PinType)
{
	PURE_VIRTUAL();
}

FName UCSPropertyGenerator::GetPinCategory(const FCSPropertyMetaData& PropertyMetaData) const
{
	static TMap<ECSPropertyType, FName> PropertyTypeToPinCategory =
	{
		{ ECSPropertyType::Bool, UEdGraphSchema_K2::PC_Boolean },
		{ ECSPropertyType::Byte, UEdGraphSchema_K2::PC_Byte },
		{ ECSPropertyType::Int, UEdGraphSchema_K2::PC_Int },
		{ ECSPropertyType::Float, UEdGraphSchema_K2::PC_Float },
		{ ECSPropertyType::Name, UEdGraphSchema_K2::PC_Name },
		{ ECSPropertyType::String, UEdGraphSchema_K2::PC_String },
		{ ECSPropertyType::Text, UEdGraphSchema_K2::PC_Text },
		{ ECSPropertyType::Enum, UEdGraphSchema_K2::PC_Byte },
		{ ECSPropertyType::Struct, UEdGraphSchema_K2::PC_Struct },
		{ ECSPropertyType::Object, UEdGraphSchema_K2::PC_Object },
		{ ECSPropertyType::WeakObject, UEdGraphSchema_K2::PC_Object },
		{ ECSPropertyType::SoftObject, UEdGraphSchema_K2::PC_Object },
		{ ECSPropertyType::ObjectPtr, UEdGraphSchema_K2::PC_Object },
		{ ECSPropertyType::MulticastInlineDelegate, UEdGraphSchema_K2::PC_MCDelegate },
		{ ECSPropertyType::Delegate, UEdGraphSchema_K2::PC_Delegate },
		{ ECSPropertyType::DefaultComponent, UEdGraphSchema_K2::PC_Object }
	};

	return PropertyTypeToPinCategory.FindRef(PropertyMetaData.Type->PropertyType);
}

UObject* UCSPropertyGenerator::GetPinSubCategoryObject(UBlueprint* Blueprint, const FCSPropertyMetaData& PropertyMetaData) const
{
	return nullptr;
}
#endif

bool UCSPropertyGenerator::CanBeHashed(const FProperty* InParam)
{
#if WITH_EDITOR
	if(InParam->IsA<FBoolProperty>())
	{
		return false;
	}

	if (InParam->IsA<FTextProperty>())
	{
		return false;
	}
	
	if (const FStructProperty* StructProperty = CastField<FStructProperty>(InParam))
	{
		return FBlueprintEditorUtils::StructHasGetTypeHash(StructProperty->Struct);
	}
#endif
	return true;
}

FProperty* UCSPropertyGenerator::NewProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData, FFieldClass* FieldClass)
{
	FName PropertyName = PropertyMetaData.Name;
	
	if (EnumHasAnyFlags(PropertyMetaData.PropertyFlags, CPF_ReturnParm))
	{
		PropertyName = "ReturnValue";
	}

	if (FieldClass == nullptr)
	{
		FieldClass = GetPropertyClass();
	}
	
	FProperty* NewProperty = static_cast<FProperty*>(FieldClass->Construct(Outer, PropertyName, RF_Public));
	NewProperty->PropertyFlags = PropertyMetaData.PropertyFlags;
	return NewProperty;
}
