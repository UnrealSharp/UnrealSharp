#pragma once

#include "CSGeneratedTypeBuilder.h"
#include "CSharpForUE/TypeGenerator/CSClass.h"
#include "MetaData/CSClassMetaData.h"

class CSHARPFORUE_API FCSGeneratedClassBuilder : public TCSGeneratedTypeBuilder<FCSClassMetaData, UCSClass>
{
	
public:

	FCSGeneratedClassBuilder(const TSharedPtr<FCSClassMetaData>& InTypeMetaData) : TCSGeneratedTypeBuilder(InTypeMetaData) {}

	// TCSGeneratedTypeBuilder interface implementation
	virtual void StartBuildingType() override;
	virtual void NewField(UCSClass* OldField, UCSClass* NewField) override;
	virtual FString GetFieldName() const override;
	// End of implementation
	
	static void* TryGetManagedFunction(UClass* Outer, const FName& MethodName);

	static UCSClass* GetFirstManagedClass(UClass* Class);
	static UClass* GetFirstNativeClass(UClass* Class);
	static UClass* GetFirstNonBlueprintClass(UClass* Class);

	static bool IsManagedType(const UClass* Class);
		
private:
	
	static void ObjectConstructor(const FObjectInitializer& ObjectInitializer);
	static void ActorComponentConstructor(const FObjectInitializer& ObjectInitializer);
	static void ActorConstructor(const FObjectInitializer& ObjectInitializer);

	static void InitialSetup(const FObjectInitializer& ObjectInitializer, TSharedPtr<FCSharpClassInfo>& ClassInfo, UCSClass*& ManagedClass);
	
	static void SetupDefaultSubobjects(const FObjectInitializer& ObjectInitializer,
		AActor* Actor,
		UClass* ActorClass,
		UCSClass* FirstManagedClass,
		const TSharedPtr<FCSharpClassInfo>& ClassInfo);
	
	static void ImplementInterfaces(UClass* ManagedClass, const TArray<FName>& Interfaces);
};
