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
	
	// UCSManagedTypeCompiler interface implementation
	virtual void Compile(UField* TypeToRecompile, const TSharedPtr<FCSManagedTypeDefinition>& ManagedTypeDefinition) const override;
	virtual TSharedPtr<FCSTypeReferenceReflectionData> CreateReflectionData() const override;
	// End of implementation
private:
	static void PurgeStruct(UCSScriptStruct* Field);
};
