#include "EnumPropertyTranslator.h"

TMap<FName, FString> FEnumPropertyTranslator::StrippedPrefixes;

FEnumPropertyTranslator::FEnumPropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers)
: FBlittableTypePropertyTranslator(InPropertyHandlers, FByteProperty::StaticClass(), "", EPU_Any)
{
	
}

bool FEnumPropertyTranslator::CanHandleProperty(const FProperty* Property) const
{
	return Property->IsA(FEnumProperty::StaticClass()) || (Property->IsA(FByteProperty::StaticClass()) && CastField<const FByteProperty>(Property)->Enum);
}

UEnum* GetEnum(const FProperty* Property)
{
	UEnum* Enum;
	if (const FEnumProperty* EnumProperty = CastField<FEnumProperty>(Property))
	{
		Enum = EnumProperty->GetEnum();
	}
	else
	{
		const FByteProperty* ByteProperty = CastFieldChecked<FByteProperty>(Property);
		Enum = ByteProperty->Enum;
	}

	check(Enum);
	return Enum;
}

FString FEnumPropertyTranslator::GetManagedType(const FProperty* Property) const
{
	return GetScriptNameMapper().GetQualifiedName(GetEnum(Property));
}

void FEnumPropertyTranslator::AddReferences(const FProperty* Property, TSet<UField*>& References) const
{
	if (const FEnumProperty* EnumProperty = CastField<const FEnumProperty>(Property))
	{
		References.Add(EnumProperty->GetEnum());
	}
	else
	{
		const FByteProperty* ByteProperty = CastFieldChecked<const FByteProperty>(Property);
		References.Add(ByteProperty->Enum);
	}
}

void FEnumPropertyTranslator::AddStrippedPrefix(const UEnum* Enum, const FString& Prefix)
{
	check(!StrippedPrefixes.Contains(Enum->GetFName()));
	StrippedPrefixes.Add(Enum->GetFName(), Prefix);
}

FString FEnumPropertyTranslator::ConvertCppDefaultParameterToCSharp(const FString& CppDefaultValue, UFunction* Function, FProperty* ParamProperty) const
{
	const UEnum* Enum = GetEnum(ParamProperty);
	const int32 Index = Enum->GetIndexByName(*CppDefaultValue);
	FString EnumName = Enum->GetNameByIndex(Index).ToString();

	const int32 ColonPos = EnumName.Find(TEXT("::"));
	
	if (ColonPos != INDEX_NONE)
	{
		EnumName = EnumName.Mid(ColonPos + 2);
	}
	
	return FString::Printf(TEXT("%s.%s"), *GetManagedType(ParamProperty), *EnumName);
}
