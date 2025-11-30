#include "ReflectionData/CSPropertyReflectionData.h"
#include "Factories/CSPropertyFactory.h"
#include "Factories/PropertyGenerators/CSPropertyGenerator.h"
#include "ReflectionData/CSFunctionReflectionData.h"

#define JSON_PARSE_GETTER_SETTER(MemberPtr, Optional) \
	{ \
		FCSFunctionReflectionData MemberPtr##Data; \
		bSuccess &= ParseObjectField(MemberPtr##Data, JsonObject, TEXT(#MemberPtr), Optional); \
		if (MemberPtr##Data.FieldName.IsValid()) \
		{ \
			MemberPtr = MakeShared<FCSFunctionReflectionData>(MemberPtr##Data); \
		} \
	}

bool FCSPropertyReflectionData::Serialize(TSharedPtr<FJsonObject> JsonObject)
{
	START_JSON_SERIALIZE
	
	CALL_SERIALIZE(FCSTypeReferenceReflectionData::Serialize(JsonObject));
	
	JSON_READ_ENUM(LifetimeCondition, IS_OPTIONAL);
	JSON_READ_ENUM(PropertyFlags, IS_OPTIONAL);
	
	JSON_READ_STRING(RepNotifyFunctionName, IS_OPTIONAL);

	ECSPropertyType PropertyType = ECSPropertyType::Unknown;
	JSON_READ_ENUM(PropertyType, IS_REQUIRED);
	
	JSON_PARSE_GETTER_SETTER(GetterMethod, IS_OPTIONAL);
	JSON_PARSE_GETTER_SETTER(SetterMethod, IS_OPTIONAL);
	
	UCSPropertyGenerator* PropertyGenerator = FCSPropertyFactory::GetPropertyGenerator(PropertyType);
	InnerType = PropertyGenerator->CreatePropertyInnerTypeData(PropertyType);

	if (!InnerType.IsValid())
	{
		UE_LOGFMT(LogUnrealSharp, Error, "Failed to create type reflection data for property '{0}' of type '{1}'", *FieldName.GetFullName().ToString(), static_cast<uint8>(PropertyType));
		SET_SUCCESS(false);
		END_JSON_SERIALIZE
	}
	
	InnerType->PropertyType = PropertyType;
	CALL_SERIALIZE(InnerType->Serialize(JsonObject));
	END_JSON_SERIALIZE
}
