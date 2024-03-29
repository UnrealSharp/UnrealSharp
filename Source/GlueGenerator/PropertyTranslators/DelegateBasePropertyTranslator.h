﻿#pragma once
#include "PropertyTranslator.h"

class FDelegateBasePropertyTranslator : public FPropertyTranslator
{
public:
	
	FDelegateBasePropertyTranslator(FCSSupportedPropertyTranslators& InPropertyHandlers, EPropertyUsage InPropertyUsage)
	: FPropertyTranslator(InPropertyHandlers, InPropertyUsage)
	{
	}

	// FPropertyTranslator interface implementation
	virtual void ExportPropertyStaticConstruction(FCSScriptBuilder& Builder,
		const FProperty* Property,
		const FString& NativePropertyName) const override;
	// End of implementation
	
};
