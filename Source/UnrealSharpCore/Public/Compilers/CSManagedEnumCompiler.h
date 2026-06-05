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
	
	// UCSManagedTypeCompiler interface implementation
	virtual void Compile(UField* TypeToRecompile, const TSharedPtr<FCSManagedTypeDefinition>& ManagedTypeDefinition) const override;
	virtual TSharedPtr<FCSTypeReferenceReflectionData> CreateReflectionData() const override;
	// End of implementation

private:
	static void PurgeEnum(UCSEnum* Enum);
};
