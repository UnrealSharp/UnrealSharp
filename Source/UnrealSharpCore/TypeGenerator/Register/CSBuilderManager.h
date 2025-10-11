#pragma once

#include "CoreMinimal.h"
#include "Subsystems/EngineSubsystem.h"
#include "CSBuilderManager.generated.h"

struct FCSManagedTypeInfo;
class UCSGeneratedTypeBuilder;


UCLASS()
class UCSTypeBuilderManager : public UObject
{
	GENERATED_BODY()
public:
	
	void Initialize();
	const UCSGeneratedTypeBuilder* GetTypeBuilder(UClass* TypeClass);

private:
	UPROPERTY(Transient)
	TArray<TObjectPtr<UCSGeneratedTypeBuilder>> TypeBuilders;
};
