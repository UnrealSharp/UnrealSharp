#pragma once

#include "CSManagedTypeCompiler.h"
#include "CSManagedDelegateCompiler.generated.h"

UCLASS()
class UCSManagedDelegateCompiler : public UCSManagedTypeCompiler
{
	GENERATED_BODY()
public:
	UCSManagedDelegateCompiler();
	
	// UCSManagedTypeCompiler interface implementation
	virtual void Recompile(UField* TypeToRecompile, const TSharedPtr<FCSManagedTypeDefinition>& ManagedTypeDefinition) const override;
	virtual TSharedPtr<FCSTypeReferenceReflectionData> CreateNewReflectionData() const override;
	// End of implementation
};
