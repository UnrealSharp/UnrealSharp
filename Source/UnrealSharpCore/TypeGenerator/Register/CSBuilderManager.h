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
	const UCSGeneratedTypeBuilder* BorrowTypeBuilder(const TSharedPtr<FCSManagedTypeInfo>& ManagedTypeInfo);

private:
	UPROPERTY(Transient)
	TMap<uint32, TObjectPtr<UCSGeneratedTypeBuilder>> TypeBuilders;
};
