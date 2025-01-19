#pragma once

#include "CSGeneratedTypeBuilder.h"
#include "UnrealSharpCore/TypeGenerator/CSClass.h"
#include "MetaData/CSClassMetaData.h"

class UNREALSHARPCORE_API FCSGeneratedClassBuilder : public TCSGeneratedTypeBuilder<FCSClassMetaData, UCSClass>
{
	
public:

	FCSGeneratedClassBuilder(const TSharedPtr<FCSClassMetaData>& InTypeMetaData) : TCSGeneratedTypeBuilder(InTypeMetaData) {}

	// TCSGeneratedTypeBuilder interface implementation
	virtual void StartBuildingType() override;
	virtual FName GetFieldName() const override;
	virtual bool ReplaceTypeOnReload() const override { return false; }
	// End of implementation

	static UCSClass* GetFirstManagedClass(UClass* Class);
	static UClass* GetFirstNativeClass(UClass* Class);
	static UClass* GetFirstNonBlueprintClass(UClass* Class);

	static void SetupDefaultSubobjects(const FObjectInitializer& ObjectInitializer,
													  AActor* Actor,
													  UClass* ActorClass,
													  UCSClass* FirstManagedClass,
													  const TSharedPtr<const FCSharpClassInfo>& ClassInfo);

#if WITH_EDITOR
	void ValidateComponentNodes(UBlueprint* Blueprint, const TSharedPtr<const FCSharpClassInfo>& ClassInfo);
#endif

	static bool IsManagedType(const UClass* Class);

	static void ManagedObjectConstructor(const FObjectInitializer& ObjectInitializer);
	static void ManagedActorConstructor(const FObjectInitializer& ObjectInitializer);

private:


	static void InitialSetup(const FObjectInitializer& ObjectInitializer, UCSClass*& OutManagedClass, TSharedPtr<const FCSharpClassInfo>& OutClassInfo);
	
	void SetupDefaultTickSettings(UObject* DefaultObject) const;
	static void ImplementInterfaces(UClass* ManagedClass, const TArray<FName>& Interfaces);
};
