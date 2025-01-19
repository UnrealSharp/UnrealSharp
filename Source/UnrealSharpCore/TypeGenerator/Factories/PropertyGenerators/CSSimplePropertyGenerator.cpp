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
	AddPinType(ECSPropertyType::Bool, UEdGraphSchema_K2::PC_Boolean);
	AddPinType(ECSPropertyType::Int8, UEdGraphSchema_K2::PC_Byte);
	AddPinType(ECSPropertyType::Int16, UEdGraphSchema_K2::PC_Int);
	AddPinType(ECSPropertyType::Int, UEdGraphSchema_K2::PC_Int);
	AddPinType(ECSPropertyType::Int64, UEdGraphSchema_K2::PC_Int64);
	AddPinType(ECSPropertyType::Byte, UEdGraphSchema_K2::PC_Byte);
	AddPinType(ECSPropertyType::UInt16, UEdGraphSchema_K2::PC_Int);
	AddPinType(ECSPropertyType::UInt32, UEdGraphSchema_K2::PC_Int);
	AddPinType(ECSPropertyType::UInt64, UEdGraphSchema_K2::PC_Int64);
	AddPinType(ECSPropertyType::Double, UEdGraphSchema_K2::PC_Double);
	AddPinType(ECSPropertyType::Float, UEdGraphSchema_K2::PC_Float);
	AddPinType(ECSPropertyType::String, UEdGraphSchema_K2::PC_String);
	AddPinType(ECSPropertyType::Name, UEdGraphSchema_K2::PC_Name);
	AddPinType(ECSPropertyType::Text, UEdGraphSchema_K2::PC_Text);
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
