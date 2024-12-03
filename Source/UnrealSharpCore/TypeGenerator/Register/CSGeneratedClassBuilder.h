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
#if WITH_EDITOR
	virtual void OnFieldReplaced(UCSClass* OldField, UCSClass* NewField) override;
#endif
	// End of implementation

	static UCSClass* GetFirstManagedClass(UClass* Class);
	static UClass* GetFirstNativeClass(UClass* Class);
	static UClass* GetFirstNonBlueprintClass(UClass* Class);

	static void SetupDefaultSubobjects(const FObjectInitializer& ObjectInitializer,
													  AActor* Actor,
													  UClass* ActorClass,
													  UCSClass* FirstManagedClass,
													  const TSharedPtr<const FCSharpClassInfo>& ClassInfo);

	static bool IsManagedType(const UClass* Class);

private:
	
	static void ManagedObjectConstructor(const FObjectInitializer& ObjectInitializer);
	static void ManagedActorConstructor(const FObjectInitializer& ObjectInitializer);

	static void InitialSetup(const FObjectInitializer& ObjectInitializer, UCSClass*& OutManagedClass, TSharedPtr<const FCSharpClassInfo>& OutClassInfo);
	
	void SetupDefaultTickSettings(UObject* DefaultObject) const;
	static void ImplementInterfaces(UClass* ManagedClass, const TArray<FName>& Interfaces);
};
