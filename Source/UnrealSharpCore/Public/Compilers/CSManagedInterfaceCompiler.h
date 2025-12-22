#pragma once

#include "CSManagedTypeCompiler.h"
#include "CSManagedInterfaceCompiler.generated.h"

class UCSInterface;

UCLASS()
class UCSManagedInterfaceCompiler : public UCSManagedTypeCompiler
{
	GENERATED_BODY()
public:
	UCSManagedInterfaceCompiler();
	
	// UCSManagedTypeCompiler interface implementation
	virtual void Recompile(UField* TypeToRecompile, const TSharedPtr<FCSManagedTypeDefinition>& ManagedTypeDefinition) const override;
	virtual TSharedPtr<FCSTypeReferenceReflectionData> CreateNewReflectionData() const override;
	// End of implementation
};
