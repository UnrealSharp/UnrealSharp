#pragma once

#include "CSManagedTypeCompiler.h"
#include "CSManagedEnumCompiler.generated.h"

class UCSEnum;

UCLASS()
class UCSManagedEnumCompiler : public UCSManagedTypeCompiler
{
	GENERATED_BODY()
public:
	UCSManagedEnumCompiler();
	
	// TCSGeneratedTypeBuilder interface implementation
	virtual void Recompile(UField* TypeToRecompile, const TSharedPtr<FCSManagedTypeDefinition>& ManagedTypeDefinition) const override;
	// End of implementation

private:
	static void PurgeEnum(UCSEnum* Enum);
};
