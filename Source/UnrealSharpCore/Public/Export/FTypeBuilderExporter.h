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
	static void NewType_Internal(char* FieldName, char* Namespace, char* AssemblyName, char* JsonString, ECSFieldType FieldType, uint8* TypeHandle);
};
