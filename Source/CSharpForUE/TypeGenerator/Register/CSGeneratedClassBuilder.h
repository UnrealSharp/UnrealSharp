#pragma once

#include "CSGeneratedTypeBuilder.h"
#include "CSharpForUE/TypeGenerator/CSClass.h"
#include "CSMetaData.h"

class CSHARPFORUE_API FCSGeneratedClassBuilder : public TCSGeneratedTypeBuilder<FClassMetaData, UCSClass>
{
	
public:

	FCSGeneratedClassBuilder(const TSharedPtr<FClassMetaData>& InTypeMetaData) : TCSGeneratedTypeBuilder(InTypeMetaData) {}

	// TCSGeneratedTypeBuilder interface implementation
	virtual void StartBuildingType() override;
	virtual void NewField(UCSClass* OldField, UCSClass* NewField) override;
	// End of implementation
	
	static void* TryGetManagedFunction(const UClass* Outer, const FName& MethodName);

	static UCSClass* GetFirstManagedClass(UClass* Class);
	static UClass* GetFirstNativeClass(UClass* Class);
	static UClass* GetFirstNonBlueprintClass(UClass* Class);

	static bool IsManagedType(const UClass* Class);
		
private:
	
	static void ObjectConstructor(const FObjectInitializer& ObjectInitializer);
	static void ActorConstructor(const FObjectInitializer& ObjectInitializer);
	
	static void SetupDefaultSubobjects(const FObjectInitializer& ObjectInitializer, AActor* Actor, const UClass* ActorClass, const TSharedPtr<FClassMetaData>& ClassMetaData);
	static void ImplementInterfaces(UClass* ManagedClass, const TArray<FString>& Interfaces);
};
