#include "BlittableStructPropertyTranslator.h"

FBlittableStructPropertyTranslator::FBlittableStructPropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers)
: FBlittableTypePropertyTranslator(InPropertyHandlers, FStructProperty::StaticClass(), "")
{

}

bool FBlittableStructPropertyTranslator::IsStructBlittable(const FCSPropertyTranslatorManager& PropertyHandlers, const UScriptStruct& Struct)
{
	int32 CalculatedPropertySize = 0;
	for (TFieldIterator<FProperty> PropIt(&Struct); PropIt; ++PropIt)
	{
		const FProperty* StructProperty = *PropIt;
		
		if (StructProperty->HasAnyPropertyFlags(CPF_BlueprintVisible) && PropertyHandlers.Find(StructProperty).IsBlittable())
		{
			CalculatedPropertySize += StructProperty->ElementSize;
		}
		else
		{
			return false;
		}
	}
	
	return CalculatedPropertySize == Struct.GetStructureSize();
}

bool FBlittableStructPropertyTranslator::CanHandleProperty(const FProperty* Property) const
{
	const FStructProperty* StructProperty = CastFieldChecked<FStructProperty>(Property);
	check(StructProperty->Struct);
	const UScriptStruct& Struct = *StructProperty->Struct;

	return IsStructBlittable(PropertyHandlers, Struct);
}

FString FBlittableStructPropertyTranslator::GetManagedType(const FProperty* Property) const
{
	const FStructProperty* StructProperty = CastFieldChecked<FStructProperty>(Property);
	check(StructProperty->Struct);
	return GetScriptNameMapper().GetQualifiedName(StructProperty->Struct);
}

void FBlittableStructPropertyTranslator::AddReferences(const FProperty* Property, TSet<UField*>& References) const
{
	const FStructProperty* StructProperty = CastFieldChecked<FStructProperty>(Property);
	References.Add(StructProperty->Struct);
}

void FBlittableStructPropertyTranslator::ExportCppDefaultParameterAsLocalVariable(FCSScriptBuilder& Builder, const FString& VariableName, const FString& CppDefaultValue, UFunction* Function, FProperty* ParamProperty) const
{
	ExportDefaultStructParameter(Builder, VariableName, CppDefaultValue, ParamProperty, *this);
}