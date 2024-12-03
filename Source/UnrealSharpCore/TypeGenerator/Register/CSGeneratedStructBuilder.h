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
#if WITH_EDITOR
	virtual void OnFieldReplaced(UCSScriptStruct* OldField, UCSScriptStruct* NewField) override;
#endif
	// End of implementation
};
