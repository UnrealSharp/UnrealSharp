#pragma once

#include "CoreMinimal.h"
#include "UObject/Object.h"
#include "UObject/ObjectMacros.h"
#include "UObject/NameTypes.h"
#include "UObject/Package.h"
#include "Misc/PackageName.h"

class FCSScriptBuilder;

extern const FName ScriptNameMetaDataKey;
extern const FName ScriptNoExportMetaDataKey;
extern const FName ScriptMethodMetaDataKey;
extern const FName ScriptMethodSelfReturnMetaDataKey;
extern const FName ScriptOperatorMetaDataKey;
extern const FName ScriptConstantMetaDataKey;
extern const FName ScriptConstantHostMetaDataKey;
extern const FName BlueprintTypeMetaDataKey;
extern const FName NotBlueprintTypeMetaDataKey;
extern const FName BlueprintSpawnableComponentMetaDataKey;
extern const FName BlueprintGetterMetaDataKey;
extern const FName BlueprintSetterMetaDataKey;
extern const FName CustomStructureParamMetaDataKey;
extern const FName HasNativeMakeMetaDataKey;
extern const FName HasNativeBreakMetaDataKey;
extern const FName NativeBreakFuncMetaDataKey;
extern const FName NativeMakeFuncMetaDataKey;
extern const FName DeprecatedPropertyMetaDataKey;
extern const FName DeprecatedFunctionMetaDataKey;
extern const FName DeprecationMessageMetaDataKey;

extern inline const TCHAR* FPropertyCallbacks = TEXT("FPropertyExporter");
extern inline const TCHAR* UClassCallbacks = TEXT("UClassExporter");
extern inline const TCHAR* CoreUObjectCallbacks = TEXT("UCoreUObjectExporter");
extern inline const TCHAR* FBoolPropertyCallbacks = TEXT("FBoolPropertyExporter");
extern inline const TCHAR* FStringCallbacks = TEXT("FStringExporter");
extern inline const TCHAR* UObjectCallbacks = TEXT("UObjectExporter");
extern inline const TCHAR* FArrayPropertyCallbacks = TEXT("FArrayPropertyExporter");
extern inline const TCHAR* UScriptStructCallbacks = TEXT("UScriptStructExporter");
extern inline const TCHAR* UFunctionCallbacks = TEXT("UFunctionExporter");

namespace ScriptGeneratorUtilities
{
	/** Is the given class marked as deprecated? */
	bool IsDeprecatedClass(const UClass* InClass, FString* OutDeprecationMessage = nullptr);

	/** Is the given property marked as deprecated? */
	bool IsDeprecatedProperty(const FProperty* InProp, FString* OutDeprecationMessage = nullptr);

	/** Is the given function marked as deprecated? */
	bool IsDeprecatedFunction(const UFunction* InFunc, FString* OutDeprecationMessage = nullptr);

	/** Should the given class be exported to scripts? */
	bool ShouldExportClass(const UClass* InClass);

	/** Should the given struct be exported to scripts? */
	bool ShouldExportStruct(const UScriptStruct* InStruct);

	/** Should the given enum be exported to scripts? */
	bool ShouldExportEnum(const UEnum* InEnum);

	/** Should the given enum entry be exported to scripts? */
	bool ShouldExportEnumEntry(const UEnum* InEnum, int32 InEnumEntryIndex);

	/** Should the given property be exported to scripts? */
	bool ShouldExportProperty(const FProperty* InProp);

	/** Should the given property be exported to scripts as editor-only data? */
	bool ShouldExportEditorOnlyProperty(const FProperty* InProp);

	/** Should the given function be exported to scripts? */
	bool ShouldExportFunction(const UFunction* InFunc);

	bool IsInterfaceFunction(UFunction* Function);

	enum EScriptNameKind : uint8
	{
		Class,
		Function,
		Property,
		Enum,
		ScriptMethod,
		Constant,
		Parameter,
		EnumValue
	};

	class FScriptNameMapper
	{
	public:
		virtual ~FScriptNameMapper() {}

		virtual FString ScriptifyName(const FString& InName, const EScriptNameKind InNameKind) const;

		virtual FString ScriptifyName(const FString& InName, const EScriptNameKind InNameKind, const TSet<FString>& ReservedNames) const;

		/** Get the native module the given field belongs to */
		FString GetFieldModule(const FField* InField) const;

		/** Get the plugin module the given field belongs to (if any) */
		FString GetFieldPlugin(const FField* InField) const;

		/** Get the script name of the given class */
		FString GetScriptClassName(const UClass* InClass) const;

		/** Get the deprecated script names of the given class */
		TArray<FString> GetDeprecatedClassScriptNames(const UClass* InClass) const;

		/** Get the script name of the given struct */
		FString GetStructScriptName(const UScriptStruct* InStruct) const;

		FString GetTypeScriptName(const UStruct* InType) const;

		/** Get the deprecated script names of the given struct */
		TArray<FString> GetDeprecatedStructScriptNames(const UScriptStruct* InStruct) const;

		/** Get the script name of the given enum */
		FString MapEnumName(const FEnumProperty* InEnum) const;

		/** Get the deprecated script names of the given enum */
		TArray<FString> GetDeprecatedEnumScriptNames(const FEnumProperty* InEnum) const;

		/** Get the script name of the given enum entry */
		FString MapEnumEntryName(const FEnumProperty* InEnum, const int32 InEntryIndex) const;

		/** Get the script name of the given delegate signature */
		FString MapDelegateName(const UFunction* InDelegateSignature) const;

		/** Get the script name of the given function */
		FString MapFunctionName(const UFunction* InFunc) const;

		/** Get the deprecated script names of the given function */
		TArray<FString> GetDeprecatedFunctionScriptNames(const UFunction* InFunc) const;

		/** Get the script name of the given function when it's hoisted as a script method */
		FString MapScriptMethodName(const UFunction* InFunc) const;

		/** Get the deprecated script names of the given function it's hoisted as a script method */
		TArray<FString> GetDeprecatedScriptMethodScriptNames(const UFunction* InFunc) const;

		/** Get the script name of the given function when it's hoisted as a script constant */
		FString MapScriptConstantName(const UFunction* InFunc) const;

		/** Get the deprecated script names of the given function it's hoisted as a script constant */
		TArray<FString> GetDeprecatedScriptConstantScriptNames(const UFunction* InFunc) const;

		/** Get the script name of the given property */
		FString MapPropertyName(const FProperty* InProp, const TSet<FString>& ReservedNames) const;

		/** Get the deprecated script names of the given property */
		TArray<FString> GetDeprecatedPropertyScriptNames(const FProperty* InProp) const;

		/** Get the script name of the given function parameter */
		FString MapParameterName(const FProperty* InProp) const;
	};

	/** Case sensitive hashing function for TSet */
	struct FCaseSensitiveStringSetFuncs : BaseKeyFuncs<FString, FString>
	{
		static FORCEINLINE const FString& GetSetKey(const FString& Element)
		{
			return Element;
		}
		static FORCEINLINE bool Matches(const FString& A, const FString& B)
		{
			return A.Equals(B, ESearchCase::CaseSensitive);
		}
		static FORCEINLINE uint32 GetKeyHash(const FString& Key)
		{
			return FCrc::StrCrc32<TCHAR>(*Key);
		}
	};

	void InitializeToolTipLocalization();
	FString GetEnumValueMetaData(const UEnum& InEnum, const TCHAR* MetadataKey, int32 ValueIndex);
	FString GetEnumValueToolTip(const UEnum& InEnum, int32 ValueIndex);
	
	FString GetFieldToolTip(const FField& InField);
	FString GetClassToolTip(const UStruct& InStruct);

	bool IsEnumValueValidWithoutPrefix(FString& RawName, const FString& Prefix);
	
	FProperty* GetFirstParam(UFunction* Function);

	void AddCheckObjectForValidity(FCSScriptBuilder& Builder);

	enum class BoolHierarchicalMetaDataMode : uint8
	{
		// any value stops the hierarchical search
		SearchStopAtAnyValue,
		// search stops when it encounters first true value, ignores false ones
		SearchStopAtTrueValue
	};
	bool GetBoolMetaDataHeirarchical(const UClass* TestClass, FName KeyName, BoolHierarchicalMetaDataMode Mode);

	bool IsBlueprintFunctionLibrary(const UClass* InClass);

	bool ParseGuidFromProjectFile(FGuid& ResultGuid, const FString& ProjectPath);
	
}