#pragma once

#include "CSGeneratedTypeBuilder.h"
#include "UnrealSharpCore/TypeGenerator/CSScriptStruct.h"
#include "MetaData/CSStructMetaData.h"

class UNREALSHARPCORE_API FCSGeneratedStructBuilder : public TCSGeneratedTypeBuilder<FCSStructMetaData, UCSScriptStruct>
{
public:
	
	FCSGeneratedStructBuilder(const TSharedPtr<FCSStructMetaData>& InTypeMetaData) : TCSGeneratedTypeBuilder(InTypeMetaData) {}

	// TCSGeneratedTypeBuilder interface implementation
	virtual void StartBuildingType() override;
	virtual void NewField(UCSScriptStruct* OldField, UCSScriptStruct* NewField) override;
	// End of implementation
};
