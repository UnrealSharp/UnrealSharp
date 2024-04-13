#pragma once

#include "PropertyTranslator.h"
#include "GlueGenerator/CSPropertyTranslatorManager.h"

class FCSPropertyTranslatorManager;
class FCSScriptBuilder;

class FSimpleTypePropertyTranslator : public FPropertyTranslator
{
public:
	
	FSimpleTypePropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers, FFieldClass* InPropertyClass,
	                              const FString& InManagedType, const FString& InMarshallerType,
	                              EPropertyUsage InPropertyUsage = EPU_Any);

	//FPropertyTranslator interface implementation
	virtual bool CanHandleProperty(const FProperty* Property) const override;
	virtual FString GetManagedType(const FProperty* Property) const override;
	virtual FString ExportMarshallerDelegates(const FProperty *Property, const FString &PropertyName) const;
	virtual FString GetMarshaller(const FProperty *Property) const;
	virtual FString GetNullReturnCSharpValue(const FProperty* ReturnProperty) const override;
	virtual FString ConvertCppDefaultParameterToCSharp(const FString& CppDefaultValue, UFunction* Function, FProperty* ParamProperty) const override;
	
	virtual void ExportMarshalToNativeBuffer(FCSScriptBuilder& Builder, const FProperty* Property, const FString& Owner,
	                                         const FString& PropertyName, const FString& DestinationBuffer,
	                                         const FString& Offset, const FString& Source) const override;
	
	virtual void ExportCleanupMarshallingBuffer(FCSScriptBuilder& Builder, const FProperty* ParamProperty,
	                                            const FString& ParamName) const override final;
	
	virtual void ExportMarshalFromNativeBuffer(FCSScriptBuilder& Builder, const FProperty* Property,
	                                           const FString& Owner, const FString& PropertyName,
	                                           const FString& AssignmentOrReturn, const FString& SourceBuffer,
	                                           const FString& Offset, bool bCleanupSourceBuffer,
	                                           bool reuseRefMarshallers) const override;
	//End of implementation
	
	// Export the default value for a struct parameter as a local variable.
	void ExportDefaultStructParameter(FCSScriptBuilder& Builder, const FString& VariableName,
	                                  const FString& CppDefaultValue, FProperty* ParamProperty,
	                                  const FPropertyTranslator& Handler) const;

protected:
	
	// Alternate ctor for subclasses overriding GetMarshalerType()
	FSimpleTypePropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers, FFieldClass* InPropertyClass,
	                              const FString& InCSharpType, EPropertyUsage InPropertyUsage = EPU_Any);
	
	// Alternate ctor for subclasses overriding GetCSharpType() and GetMarshalerType()
	FSimpleTypePropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers, FFieldClass* InPropertyClass,
	                              EPropertyUsage InPropertyUsage = EPU_Any);

private:
	
	FFieldClass* PropertyClass;
	FString ManagedType;
	FString MarshallerType;
	
};
