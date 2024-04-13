#pragma once
#include "PropertyTranslator.h"

class FCSPropertyTranslatorManager;

class FArrayPropertyTranslator : public FPropertyTranslator
{
public:
	
	explicit FArrayPropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers);

	//FPropertyTranslator interface implementation
	virtual bool CanHandleProperty(const FProperty* Property) const override;
	virtual void AddReferences(const FProperty* Property, TSet<UField*>& References) const override;
	virtual FString GetManagedType(const FProperty* Property) const override;
	virtual void ExportPropertyStaticConstruction(FCSScriptBuilder& Builder, const FProperty* Property, const FString& NativePropertyName) const override;
	virtual void ExportParameterStaticConstruction(FCSScriptBuilder& Builder, const FString& CSharpMethodName, const FProperty* Parameter) const override;
	virtual FString ExportInstanceMarshallerVariables(const FProperty *Property, const FString &PropertyName) const override;
	virtual FString ExportMarshallerDelegates(const FProperty *Property, const FString &PropertyName) const override;
protected:
	virtual void ExportPropertyVariables(FCSScriptBuilder& Builder, const FProperty* Property, const FString& PropertyName) const override;
	virtual void ExportParameterVariables(FCSScriptBuilder& Builder, UFunction* Function, const FString& CSharpMethodName, FProperty* ParamProperty, const FString& CSharpPropertyName) const override;
	virtual void ExportPropertyGetter(FCSScriptBuilder& Builder, const FProperty* Property, const FString& PropertyName) const override;
	virtual void ExportMarshalToNativeBuffer(FCSScriptBuilder& Builder, const FProperty* Property, const FString &Owner, const FString& PropertyName, const FString& DestinationBuffer, const FString& Offset, const FString& Source) const override;
	virtual void ExportCleanupMarshallingBuffer(FCSScriptBuilder& Builder, const FProperty* ParamProperty, const FString& ParamName) const override;
	virtual void ExportMarshalFromNativeBuffer(FCSScriptBuilder& Builder, const FProperty* Property, const FString &Owner, const FString& PropertyName, const FString& AssignmentOrReturn, const FString& SourceBuffer, const FString& Offset, bool bCleanupSourceBuffer, bool reuseRefMarshallers) const override;
	virtual FString GetNullReturnCSharpValue(const FProperty* ReturnProperty) const override;

	// Array properties don't need a setter - all modifications should occur through the IList interface of the wrapper class.
	virtual bool IsSetterRequired() const override { return false; }
	//End of implementation

	FString GetWrapperInterface(const FProperty* Property) const;
	FString GetWrapperType(const FProperty* Property) const;
};
