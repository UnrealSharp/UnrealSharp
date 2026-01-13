#pragma once

#include "CoreMinimal.h"
#include "CSManagedTypeInterface.h"
#include "Engine/BlueprintGeneratedClass.h"
#include "CSClass.generated.h"

UCLASS(MinimalAPI)
class UCSClass : public UBlueprintGeneratedClass, public ICSManagedTypeInterface
{
	GENERATED_BODY()
public:
	UNREALSHARPCORE_API static void ManagedObjectConstructor(const FObjectInitializer& ObjectInitializer);
	
#if WITH_EDITOR
	// UObject interface
	virtual void PostDuplicate(bool bDuplicateForPIE) override;
	virtual void PurgeClass(bool bRecompilingOnLoad) override;
	// End of UObject interface
	
	void SetOwningBlueprint(UBlueprint* InOwningBlueprint)
	{
		OwningBlueprint = InOwningBlueprint;
		ClassGeneratedBy = InOwningBlueprint;
	}

	UBlueprint* GetOwningBlueprint() const 
	{
		return OwningBlueprint;
	}
	
	void SetDeferredCreation(bool bInDeferredCreation) { bDeferredCreation = bInDeferredCreation; }
	bool IsCreationDeferred() const { return bDeferredCreation; }
#endif
	
private:
#if WITH_EDITORONLY_DATA
	UPROPERTY(Transient)
	TObjectPtr<UBlueprint> OwningBlueprint;
	
	bool bDeferredCreation = true;
#endif
};