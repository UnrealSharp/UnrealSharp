#include "ReflectionData/CSPropertyReflectionData.h"
#include "Factories/CSPropertyFactory.h"
#include "Factories/PropertyGenerators/CSPropertyGenerator.h"

bool FCSPropertyReflectionData::Serialize(TSharedPtr<FJsonObject> JsonObject)
{
	START_JSON_SERIALIZE
	
	CALL_SERIALIZE(FCSTypeReferenceReflectionData::Serialize(JsonObject));

	JSON_READ_STRING(BlueprintGetter, IS_OPTIONAL);
	JSON_READ_STRING(BlueprintSetter, IS_OPTIONAL);
	
	JSON_READ_ENUM(LifetimeCondition, IS_OPTIONAL);
	JSON_READ_ENUM(PropertyFlags, IS_OPTIONAL);
	
	JSON_READ_STRING(RepNotifyFunctionName, IS_OPTIONAL);

	ECSPropertyType PropertyType = ECSPropertyType::Unknown;
	JSON_READ_ENUM(PropertyType, IS_REQUIRED);
	
	UCSPropertyGenerator* PropertyGenerator = FCSPropertyFactory::GetPropertyGenerator(PropertyType);
	InnerType = PropertyGenerator->CreatePropertyInnerTypeData(PropertyType);

	if (InnerType.IsValid())
	{
		InnerType->PropertyType = PropertyType;
		CALL_SERIALIZE(InnerType->Serialize(JsonObject));
	}
	else
	{
		UE_LOGFMT(LogUnrealSharp, Error, "Failed to create type reflection data for property '{0}' of type '{1}'", *FieldName.GetFullName().ToString(), static_cast<uint8>(PropertyType));
		SET_SUCCESS(false);
	}

	END_JSON_SERIALIZE
}
