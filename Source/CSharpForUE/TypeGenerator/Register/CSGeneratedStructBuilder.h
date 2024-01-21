#pragma once

#include "CSGeneratedTypeBuilder.h"
#include "CSTypeRegistry.h"
#include "CSharpForUE/TypeGenerator/CSScriptStruct.h"

struct FStructMetaData;

class CSHARPFORUE_API FCSGeneratedStructBuilder : public TCSGeneratedTypeBuilder<FStructMetaData, UCSScriptStruct>
{
public:
	
	FCSGeneratedStructBuilder(const TSharedPtr<FStructMetaData>& InTypeMetaData) : TCSGeneratedTypeBuilder(InTypeMetaData) {}

	// TCSGeneratedTypeBuilder interface implementation
	virtual void StartBuildingType() override;
	virtual void NewField(UCSScriptStruct* OldField, UCSScriptStruct* NewField) override;
	// End of implementation
};
