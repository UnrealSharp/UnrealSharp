#pragma once

#include "CSManagedTypeCompiler.h"
#include "CSManagedStructCompiler.generated.h"

class UCSScriptStruct;

UCLASS()
class UCSManagedStructCompiler : public UCSManagedTypeCompiler
{
	GENERATED_BODY()
public:
	UCSManagedStructCompiler();
	
	// TCSGeneratedTypeBuilder interface implementation
	virtual void Recompile(UField* TypeToRecompile, const TSharedPtr<FCSManagedTypeDefinition>& ManagedTypeDefinition) const override;
	// End of implementation
private:
	static void PurgeStruct(UCSScriptStruct* Field);
};
