#pragma once

#include "CSGeneratedTypeBuilder.h"
#include "TypeGenerator/CSClass.h"
#include "MetaData/CSClassMetaData.h"

class UNREALSHARPCORE_API FCSGeneratedClassBuilder : public TCSGeneratedTypeBuilder<FCSClassMetaData, UCSClass>
{
	
public:

	FCSGeneratedClassBuilder(const TSharedPtr<FCSClassMetaData>& InTypeMetaData, const TSharedPtr<FCSAssembly>& InOwningAssembly);

	// TCSGeneratedTypeBuilder interface implementation
	virtual void RebuildType() override;
#if WITH_EDITOR
	virtual void UpdateType() override;
#endif
	virtual FName GetFieldName() const override;
	// End of implementation
	
	static void ManagedObjectConstructor(const FObjectInitializer& ObjectInitializer);
	static void ImplementInterfaces(UClass* ManagedClass, const TArray<FCSTypeReferenceMetaData>& Interfaces);
	static void SetConfigName(UClass* ManagedClass, const TSharedPtr<const FCSClassMetaData>& TypeMetaData);
	static void SetupDefaultTickSettings(UObject* DefaultObject, const UClass* Class);

	static void TryRegisterDynamicSubsystem(UClass* ManagedClass);
	static void TryUnregisterDynamicSubsystem(UClass* ManagedClass);

private:
#if WITH_EDITOR
	void CreateBlueprint(UClass* SuperClass);
	void CreateClassEditor(UClass* SuperClass);
#endif
	void CreateClass(UClass* SuperClass);
	
	TMap<UClass*, UClass*> RedirectClasses;
};
