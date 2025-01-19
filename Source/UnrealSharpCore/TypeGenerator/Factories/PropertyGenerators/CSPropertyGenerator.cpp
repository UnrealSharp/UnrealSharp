#include "CSPropertyGenerator.h"

#include "TypeGenerator/Register/CSTypeRegistry.h"

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

bool UCSPropertyGenerator::SupportsPropertyType(ECSPropertyType InPropertyType) const
{
	ECSPropertyType PropertyType = GetPropertyType();
	check(PropertyType != ECSPropertyType::Unknown);
	return PropertyType == InPropertyType;
}

TSharedPtr<FCSUnrealType> UCSPropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	PURE_VIRTUAL();
	return nullptr;
}

#if WITH_EDITOR
void UCSPropertyGenerator::CreatePropertyEditor(UBlueprint* Blueprint, const FCSPropertyMetaData& PropertyMetaData)
{
	FBPVariableDescription NewVariable;
	NewVariable.PropertyFlags = PropertyMetaData.PropertyFlags;
	NewVariable.VarName = PropertyMetaData.Name;
	NewVariable.VarGuid = FGuid::NewGuid();
	NewVariable.VarType = GetPinType(PropertyMetaData.Type->PropertyType, PropertyMetaData);
	NewVariable.RepNotifyFunc = PropertyMetaData.RepNotifyFunctionName;
	NewVariable.ReplicationCondition = PropertyMetaData.LifetimeCondition;

	for (FBPVariableDescription& Variable : Blueprint->NewVariables)
	{
		if (Variable.VarName == NewVariable.VarName && Variable.VarType == NewVariable.VarType)
		{
			FGuid CurrentGuid = Variable.VarGuid;
			Variable = NewVariable;
			Variable.VarGuid = CurrentGuid;
			return;
		}
	}
	
	Blueprint->NewVariables.Add(NewVariable);
}

FEdGraphPinType UCSPropertyGenerator::GetPinType(ECSPropertyType PropertyType, const FCSPropertyMetaData& MetaData) const
{
	PURE_VIRTUAL();
	return FEdGraphPinType();
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

FProperty* UCSPropertyGenerator::NewProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData, const FFieldClass* FieldClass)
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


