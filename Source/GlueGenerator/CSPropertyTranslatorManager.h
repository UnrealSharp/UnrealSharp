#pragma once

#include "UObject/Field.h"

class FCSInclusionLists;
class FCSNameMapper;
class FScriptBuilder;
class FPropertyTranslator;
class FNullPropertyTranslator;

enum EPropertyUsage : uint8
{
	EPU_None = 0x00,
	EPU_Property = 0x01,
	EPU_Parameter = 0x02,
	EPU_ReturnValue = 0x04,
	EPU_ArrayInner = 0x08,
	EPU_StructProperty = 0x10,
	EPU_OverridableFunctionParameter = 0x20,
	EPU_OverridableFunctionReturnValue = 0x40,
	EPU_StaticArrayProperty = 0x80,
	EPU_Any = 0xFF,
};

class FCSPropertyTranslatorManager
{
public:
	
	virtual ~FCSPropertyTranslatorManager() = default;
	FCSPropertyTranslatorManager(const FCSNameMapper& InNameMapper, FCSInclusionLists& CodeGenerator);

	const FPropertyTranslator& Find(const FProperty* Property) const;
	const FPropertyTranslator& Find(UFunction* Property) const;

	bool IsStructBlittable(const UScriptStruct& ScriptStruct) const;

	const FCSNameMapper& GetScriptNameMapper() const { return NameMapper; }
	
private:
	
	const FCSNameMapper& NameMapper;
	TUniquePtr<FNullPropertyTranslator> NullHandler;
	TMap<FName, TArray<FPropertyTranslator*>> TranslatorMap;

	void AddPropertyTranslator(FFieldClass* PropertyClass, FPropertyTranslator* Handler);
	void AddBlittablePropertyTranslator(FFieldClass* PropertyClass, const FString& CSharpType);
	void AddBlittableCustomStructPropertyTranslator(const FString& UnrealName, const FString& CSharpName, FCSInclusionLists& Blacklist);
	void AddCustomStructPropertyTranslator(const FString& UnrealName, const FString& CSharpName, FCSInclusionLists& Blacklist);

};
