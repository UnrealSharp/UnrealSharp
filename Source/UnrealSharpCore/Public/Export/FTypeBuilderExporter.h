#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "UObject/Object.h"
#include "FTypeBuilderExporter.generated.h"

struct FCSFunctionReflectionData;
struct FCSEnumReflectionData;
struct FCSTypeReferenceReflectionData;
struct FCSClassReflectionData;
struct FCSTemplateType;
struct FCSDefaultComponentType;
struct FCSFieldType;
struct FCSPropertyReflectionData;
struct FCSClassBaseReflectionData;
enum class ECSPropertyType : uint8;
struct FCSStructReflectionData;
enum class ECSFieldType : uint8;
enum ECSStructureState : uint8;

UCLASS()
class UFTypeBuilderExporter : public UObject
{
	GENERATED_BODY()
public:
	UNREALSHARP_FUNCTION()
	static void RegisterManagedType_Native(char* FieldName, char* Namespace, char* AssemblyName, char* JsonString, ECSFieldType FieldType, uint8* TypeHandle);
};
