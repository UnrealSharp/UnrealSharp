#pragma once

#include "CoreMinimal.h"
#include "CSNameMapper.h"
#include "CSModule.h"
#include "CSGlueGeneratorFileManager.h"
#include "CSInclusionLists.h"
#include "UObject/Stack.h"
#include "PropertyTranslators/CSSupportedPropertyTranslators.h"

class GLUEGENERATOR_API FCSGenerator
{
	
public:

	static FCSGenerator& Get()
	{
		static FCSGenerator Instance;
		return Instance;
	}

	FCSGenerator(): NameMapper(this)
	{
		
	}
	
	void StartGenerator(const FString& OutputDirectory);

	// Public methods
	void GenerateGlueForTypes(TArray<UObject*>& ObjectsToProcess);
	void GenerateGlueForType(UObject* Object, bool bForceExport = false);
	
	void OnModulesChanged(FName InModuleName, EModuleChangeReason InModuleChangeReason);
	
	void GetExportedProperties(TSet<FProperty*>& ExportedProperties, const UStruct* Struct);
	void GetExportedFunctions(TSet<UFunction*>& ExportedFunctions, TSet<UFunction*>& ExportedOverridableFunctions, const UClass* Class);
	void GetExportedStructs(TSet<UScriptStruct*>& ExportedStructs) const;
	
	void ExportMirrorStructMarshalling(FCSScriptBuilder& Builder, const UScriptStruct* Struct, TSet<FProperty*> ExportedProperties) const;

	void ExportClass(UClass* Class, FCSScriptBuilder& Builder);
	void ExportStruct(UScriptStruct* Struct, FCSScriptBuilder& Builder);
	void ExportEnum(UEnum* Enum, FCSScriptBuilder& Builder);
	void ExportInterface(UClass* Interface, FCSScriptBuilder& Builder);
	
	bool CanExportClass(UClass* Class) const;
	bool CanDeriveFromNativeClass(UClass* Class);

	FString GetCSharpEnumType(EPropertyType PropertyType) const;
	
	bool CanExportFunction(const UStruct* Struct, const UFunction* Function) const;
	bool CanExportFunctionParameters(const UFunction* Function) const;
	bool CanExportParameter(const FProperty* Property) const;
	bool CanExportReturnValue(const FProperty* Property) const;
	bool CanExportOverridableParameter(const FProperty* Property);
	bool CanExportOverridableReturnValue(const FProperty* Property);
	
	bool CanExportProperty(const UStruct* Struct, const FProperty* Property) const;
	bool CanExportPropertyShared(const FProperty* Property) const;
		
	void ExportStructMarshaller(FCSScriptBuilder& Builder, const UScriptStruct* Struct);

	// Helper methods
	FString GetSuperClassName(const UClass* Class) const;
	void SaveTypeGlue(const UObject* Object, const FCSScriptBuilder& ScriptBuilder);
	void SaveGlue(const FCSModule* Bindings, const FString& Filename, const FString& GeneratedGlue);
	void SaveModuleGlue(UPackage* Package, const FString& GeneratedGlue);
	
	// Exporter methods
	
	void ExportClassProperties(FCSScriptBuilder& Builder, const UClass* Class, TSet<FProperty*>& ExportedProperties);
	void ExportStaticConstructor(FCSScriptBuilder& Builder,  const UStruct* Struct,const TSet<FProperty*>& ExportedProperties,  const TSet<UFunction*>& ExportedFunctions, const TSet<UFunction*>& ExportedOverrideableFunctions);
	void ExportClassFunctions(FCSScriptBuilder& Builder, const UClass* Class, const TSet<UFunction*>& ExportedFunctions);
	void ExportInterfaceFunctions(FCSScriptBuilder& Builder, const UClass* Class, const TSet<UFunction*>& ExportedFunctions) const;
	void ExportPropertiesStaticConstruction(FCSScriptBuilder& Builder, const TSet<FProperty*>& ExportedProperties);
	void ExportClassFunctionsStaticConstruction(FCSScriptBuilder& Builder, const TSet<UFunction*>& ExportedFunctions);
	void ExportClassOverridableFunctionsStaticConstruction(FCSScriptBuilder& Builder, const TSet<UFunction*>& ExportedOverrideableFunctions) const;
	void ExportClassFunctionStaticConstruction(FCSScriptBuilder& Builder, const UFunction *Function);
	void ExportDelegateFunctionStaticConstruction(FCSScriptBuilder& Builder, const UFunction *Function);
	void ExportClassOverridableFunctions(FCSScriptBuilder& Builder, const TSet<UFunction*>& ExportedOverridableFunctions);

	void ExportStructProperties(FCSScriptBuilder& Builder, const UStruct* Struct, const TSet<FProperty*>& ExportedProperties, bool bSuppressOffsets) const;

	void RegisterClassToModule(const UObject* Struct);

	const FString& GetNamespace(const UObject* Object);

	FCSModule& FindOrRegisterModule(const UObject* Object);

	// Public data structures
	struct ExtensionMethod
	{
		UClass* OverrideClassBeingExtended;
		UFunction* Function;
		FProperty* SelfParameter;
	};

protected:
	static bool GetExtensionMethodInfo(ExtensionMethod& Info, UFunction& Function);

	FString GeneratedScriptsDirectory;
	
	static FName AllowableBlueprintVariableType;
	static FName NotAllowableBlueprintVariableType;
	static FName BlueprintSpawnableComponent;
	static FName Blueprintable;
	static FName BlueprintFunctionLibrary;
	
	static FName ProjectGeneratedClassesDir;

	TUniquePtr<FCSSupportedPropertyTranslators> PropertyTranslators;

	FCSNameMapper NameMapper;
	FCSGlueGeneratorFileManager GeneratedFileManager;
	
	FCSInclusionLists Whitelist;
	FCSInclusionLists Blacklist;
	FCSInclusionLists BlueprintInternalWhitelist;
	FCSInclusionLists OverrideInternalList;
	FCSInclusionLists Greylist;

	TMap<FName, TArray<ExtensionMethod>> ExtensionMethods;
	
	TMap<FName, FCSModule> CSharpBindingsModules;

	TSet<UObject*> ExportedTypes;
	
	bool Initialized = false;

private:
	// Private properties
	typedef TMap<FName, int32> UnhandledPropertyCounts;
	mutable UnhandledPropertyCounts UnhandledProperties;
	mutable UnhandledPropertyCounts UnhandledParameters;
	mutable UnhandledPropertyCounts UnhandledReturnValues;
	mutable UnhandledPropertyCounts UnhandledOverridableParameters;
	mutable UnhandledPropertyCounts UnhandledOverridableReturnValues;
};
