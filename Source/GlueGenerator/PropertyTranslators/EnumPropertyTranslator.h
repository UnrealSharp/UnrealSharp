#pragma once
#include "BlittableTypePropertyTranslator.h"

class FEnumPropertyTranslator : public FBlittableTypePropertyTranslator
{
public:
	
	explicit FEnumPropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers);

	static void AddStrippedPrefix(const UEnum* Enum, const FString& Prefix);
	
	//FPropertyTranslator interface implementation
	virtual bool CanHandleProperty(const FProperty* Property) const override;
	virtual FString GetManagedType(const FProperty* Property) const override;
	virtual void AddReferences(const FProperty* Property, TSet<UField*>& References) const override;
	virtual FString ConvertCppDefaultParameterToCSharp(const FString& CppDefaultValue, UFunction* Function, FProperty* ParamProperty) const override;
	//End of implementation

private:
	
	static TMap<FName, FString> StrippedPrefixes;

	static FString GetEnumName();
	
};
