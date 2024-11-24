#include "CSSimplePropertyGenerator.h"

UCSSimplePropertyGenerator::UCSSimplePropertyGenerator(FObjectInitializer const& ObjectInitializer): Super(ObjectInitializer)
{
	TypeToFieldClass =
	{
		{ ECSPropertyType::Bool, FBoolProperty::StaticClass() },
		{ ECSPropertyType::Int8, FInt8Property::StaticClass() },
		{ ECSPropertyType::Int16, FInt16Property::StaticClass() },
		{ ECSPropertyType::Int, FIntProperty::StaticClass() },
		{ ECSPropertyType::Int64, FInt64Property::StaticClass() },
		{ ECSPropertyType::Byte, FByteProperty::StaticClass() },
		{ ECSPropertyType::UInt16, FUInt16Property::StaticClass() },
		{ ECSPropertyType::UInt32, FUInt32Property::StaticClass() },
		{ ECSPropertyType::UInt64, FUInt64Property::StaticClass() },
		{ ECSPropertyType::Double, FDoubleProperty::StaticClass() },
		{ ECSPropertyType::Float, FFloatProperty::StaticClass() },
		{ ECSPropertyType::String, FStrProperty::StaticClass() },
		{ ECSPropertyType::Name, FNameProperty::StaticClass() },
		{ ECSPropertyType::Text, FTextProperty::StaticClass() },
	};

#if WITH_EDITOR
	PropertyTypeToPinCategory =
	{
		{ ECSPropertyType::Bool, UEdGraphSchema_K2::PC_Boolean },
		{ ECSPropertyType::Int8, UEdGraphSchema_K2::PC_Byte },
		{ ECSPropertyType::Int16, UEdGraphSchema_K2::PC_Int },
		{ ECSPropertyType::Int, UEdGraphSchema_K2::PC_Int },
		{ ECSPropertyType::Int64, UEdGraphSchema_K2::PC_Int64 },
		{ ECSPropertyType::Byte, UEdGraphSchema_K2::PC_Byte },
		{ ECSPropertyType::UInt16, UEdGraphSchema_K2::PC_Int },
		{ ECSPropertyType::UInt32, UEdGraphSchema_K2::PC_Int },
		{ ECSPropertyType::UInt64, UEdGraphSchema_K2::PC_Int },
		{ ECSPropertyType::Double, UEdGraphSchema_K2::PC_Float },
		{ ECSPropertyType::Float, UEdGraphSchema_K2::PC_Float },
		{ ECSPropertyType::String, UEdGraphSchema_K2::PC_String },
		{ ECSPropertyType::Name, UEdGraphSchema_K2::PC_Name },
		{ ECSPropertyType::Text, UEdGraphSchema_K2::PC_Text },
	};
#endif
}

bool UCSSimplePropertyGenerator::SupportsPropertyType(ECSPropertyType InPropertyType) const
{
	return TypeToFieldClass.Contains(InPropertyType);
}

FFieldClass* UCSSimplePropertyGenerator::GetPropertyClass()
{
	return TypeToFieldClass[GetPropertyType()];
}
