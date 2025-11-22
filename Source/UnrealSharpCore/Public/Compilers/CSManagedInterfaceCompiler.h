#pragma once

#include "CSManagedTypeCompiler.h"
#include "CSManagedInterfaceCompiler.generated.h"

class UCSInterface;

UCLASS()
class UNREALSHARPCORE_API UCSManagedInterfaceCompiler : public UCSManagedTypeCompiler
{
	GENERATED_BODY()
public:
	UCSManagedInterfaceCompiler();
	
	// UCSManagedTypeCompiler interface implementation
	virtual void Recompile(UField* TypeToRecompile, const TSharedPtr<FCSManagedTypeDefinition>& ManagedTypeDefinition) const override;
	// End of implementation
};
