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
	virtual UCSClass* CreateField(UPackage* Package, FName FieldName) override;
	// End of implementation

	static UCSClass* GetFirstManagedClass(UClass* Class);
	static UClass* GetFirstNativeClass(UClass* Class);
	static UClass* GetFirstNonBlueprintClass(UClass* Class);

	static bool IsManagedType(const UClass* Class);

private:
	
	static void ManagedObjectConstructor(const FObjectInitializer& ObjectInitializer);
	static void SetupTick(UCSClass* ManagedClass);
	static void ImplementInterfaces(UClass* ManagedClass, const TArray<FName>& Interfaces);

	void SetupDefaultSubobjects(const TSharedPtr<FCSharpClassInfo>& ClassInfo);
	USCS_Node* CreateNode(UClass* NewComponentClass, FName NewComponentVariableName);
};
