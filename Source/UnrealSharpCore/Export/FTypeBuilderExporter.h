#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "UObject/Object.h"
#include "FTypeBuilderExporter.generated.h"

struct FCSFunctionMetaData;
struct FCSEnumMetaData;
struct FCSTypeReferenceMetaData;
struct FCSClassMetaData;
struct FCSTemplateType;
struct FCSDefaultComponentMetaData;
struct FCSFieldTypePropertyMetaData;
struct FCSPropertyMetaData;
struct FCSClassBaseMetaData;
enum class ECSPropertyType : uint8;
struct FCSStructMetaData;
enum class ECSFieldType : uint8;
enum ECSStructureState : uint8;

UCLASS()
class UFTypeBuilderExporter : public UObject
{
	GENERATED_BODY()
public:
	UNREALSHARP_FUNCTION()
	static FCSTypeReferenceMetaData* NewType_Internal(TCHAR* FieldName,
		TCHAR* Namespace,
		TCHAR* AssemblyName,
		int64 LastModifiedTime,
		ECSFieldType FieldType,
		uint8* TypeHandle,
		bool& NeedsRebuild);

	UNREALSHARP_FUNCTION()
	static void InitMetaData_Internal(FCSTypeReferenceMetaData* Owner, int32 Count);

	UNREALSHARP_FUNCTION()
	static void AddMetaData_Internal(FCSTypeReferenceMetaData* Owner, TCHAR* Key, TCHAR* Value);

	UNREALSHARP_FUNCTION()
	static void ModifyClass_Internal(FCSClassBaseMetaData* Owner, TCHAR* Name, TCHAR* Namespace, TCHAR* AssemblyName, TCHAR* ConfigName, EClassFlags Flags);

	UNREALSHARP_FUNCTION()
	static TArray<FCSPropertyMetaData>* InitializePropertiesFromTemplate_Internal(FCSPropertyMetaData* Owner, int32 NumProperties);

	UNREALSHARP_FUNCTION()
	static TArray<FCSPropertyMetaData>* InitializePropertiesFromStruct_Internal(FCSStructMetaData* Owner, int32 NumProperties);

	UNREALSHARP_FUNCTION()
	static FCSPropertyMetaData* MakeProperty_Internal(TArray<FCSPropertyMetaData>* Owner, uint8 PropertyTypeInt,
		TCHAR* Name,
		uint64 FlagsInt,
		TCHAR* RepNotifyFuncName,
		int32 InArrayDim,
		int32 LifetimeConditionInt,
		TCHAR* InBlueprintSetter,
		TCHAR* InBlueprintGetter);

	UNREALSHARP_FUNCTION()
	static void ModifyFieldProperty_Internal(FCSPropertyMetaData* OutMetaData, TCHAR* Name, TCHAR* Namespace, TCHAR* AssemblyName);

	UNREALSHARP_FUNCTION()
	static void ModifyDefaultComponent_Internal(FCSPropertyMetaData* OutMetaData, bool IsRootComponent, const TCHAR* AttachmentComponent, const TCHAR* AttachmentSocket);

	UNREALSHARP_FUNCTION()
	static void ReserveFunctions_Internal(FCSClassBaseMetaData* Owner, int32 NumFunctions);
	
	UNREALSHARP_FUNCTION()
	static FCSFunctionMetaData* MakeFunction_Internal(FCSClassBaseMetaData* Owner, const TCHAR* Name, EFunctionFlags Flags, int32 NumParams);

	UNREALSHARP_FUNCTION()
	static void ReserveOverrides_Internal(FCSClassMetaData* Owner, int32 NumOverrides);

	UNREALSHARP_FUNCTION()
	static void MakeOverride_Internal(FCSClassMetaData* Owner, const TCHAR* NativeName);

	UNREALSHARP_FUNCTION()
	static void ReserveEnumValues_Internal(FCSEnumMetaData* Owner, int32 NumValues);

	UNREALSHARP_FUNCTION()
	static void AddEnumValue_Internal(FCSEnumMetaData* Owner, const TCHAR* Name);

	UNREALSHARP_FUNCTION()
	static void ReserveInterfaces_Internal(FCSClassMetaData* Owner, int32 NumInterfaces);

	UNREALSHARP_FUNCTION()
	static void AddInterface_Internal(FCSClassMetaData* Owner, TCHAR* Name, TCHAR* Namespace, TCHAR* AssemblyName);
};
