#pragma once

#include "CSGeneratedTypeBuilder.h"
#include "UnrealSharpCore/TypeGenerator/CSScriptStruct.h"
#include "MetaData/CSStructMetaData.h"

class UNREALSHARPCORE_API FCSGeneratedStructBuilder : public TCSGeneratedTypeBuilder<FCSStructMetaData, UCSScriptStruct>
{
public:
	
	FCSGeneratedStructBuilder(const TSharedPtr<FCSStructMetaData>& InTypeMetaData, const TSharedPtr<FCSAssembly>& InOwningAssembly) : TCSGeneratedTypeBuilder(InTypeMetaData, InOwningAssembly) {}

	// TCSGeneratedTypeBuilder interface implementation
	virtual void RebuildType() override;
#if WITH_EDITOR
	virtual void UpdateType() override;
#endif
	// End of implementation

private:
	void PurgeStruct();
};
