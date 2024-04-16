#pragma once

#include "PropertyTranslator.h"
#include "GlueGenerator/CSPropertyTranslatorManager.h"

class FDelegateBasePropertyTranslator : public FPropertyTranslator
{
public:
	
	FDelegateBasePropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers, EPropertyUsage InPropertyUsage)
	: FPropertyTranslator(InPropertyHandlers, InPropertyUsage)
	{
	}

	// FPropertyTranslator interface implementation
	virtual void ExportPropertyStaticConstruction(FCSScriptBuilder& Builder,
		const FProperty* Property,
		const FString& NativePropertyName) const override;
	// End of implementation

	static FString GetDelegateName(const UFunction* SignatureFunction);
	
};
