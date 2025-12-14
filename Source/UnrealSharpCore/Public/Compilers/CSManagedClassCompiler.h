#pragma once

#include "CSManagedTypeCompiler.h"
#include "ReflectionData/CSTypeReferenceReflectionData.h"
#include "CSManagedClassCompiler.generated.h"

struct FCSClassReflectionData;
class UCSClass;

UCLASS()
class UNREALSHARPCORE_API UCSManagedClassCompiler : public UCSManagedTypeCompiler
{
	GENERATED_BODY()
public:
	UCSManagedClassCompiler();
	
	// UCSManagedTypeCompiler interface implementation
	virtual void Recompile(UField* TypeToRecompile, const TSharedPtr<FCSManagedTypeDefinition>& ManagedTypeDefinition) const override;
	virtual FString GetFieldName(TSharedPtr<const FCSTypeReferenceReflectionData>& ReflectionData) const override;
	// End of implementation
	
	static void ImplementInterfaces(UClass* ManagedClass, const TArray<FCSTypeReferenceReflectionData>& Interfaces);
	static void SetConfigName(UClass* ManagedClass, const TSharedPtr<const FCSClassReflectionData>& ClassReflectionData);
	static void SetupDefaultTickSettings(UObject* DefaultObject, const UClass* Class);

	static void ActivateSubsystem(TSubclassOf<USubsystem> SubsystemClass);
	static void DeactivateSubsystem(TSubclassOf<USubsystem> SubsystemClass);
	
#if WITH_EDITOR
	static void RefreshClassActions(UClass* ClassToRefresh);
#endif

private:
#if WITH_EDITOR
	static void CreateOrUpdateOwningBlueprint(TSharedPtr<FCSClassReflectionData> ClassReflectionData, UCSClass* Field, UClass* SuperClass);
#endif
	static void CompileClass(TSharedPtr<FCSClassReflectionData> ClassReflectionData, UCSClass* Field, UClass* SuperClass);

	UClass* TryRedirectSuperClass(TSharedPtr<FCSClassReflectionData> ClassReflectionData, UClass* SuperClass) const;
	
	TMap<TObjectKey<UClass>, TWeakObjectPtr<UClass>> RedirectClasses;
};
