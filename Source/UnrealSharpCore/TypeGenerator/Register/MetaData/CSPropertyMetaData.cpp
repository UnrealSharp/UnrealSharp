#include "CSPropertyMetaData.h"
#include "TypeGenerator/Register/CSMetaDataUtils.h"

void FCSPropertyMetaData:: SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FCSMemberMetaData::SerializeFromJson(JsonObject);
	
	PropertyFlags = FCSMetaDataUtils::GetFlags<EPropertyFlags>(JsonObject,"PropertyFlags");
	LifetimeCondition = FCSMetaDataUtils::GetFlags<ELifetimeCondition>(JsonObject,"LifetimeCondition");
	
	JsonObject->TryGetStringField(TEXT("BlueprintGetter"), BlueprintGetter);
	JsonObject->TryGetStringField(TEXT("BlueprintSetter"), BlueprintSetter);
	JsonObject->TryGetBoolField(TEXT("IsArray"), IsArray);

	FString RepNotifyFunctionNameStr;
	if (JsonObject->TryGetStringField(TEXT("RepNotifyFunctionName"), RepNotifyFunctionNameStr))
	{
		RepNotifyFunctionName = *RepNotifyFunctionNameStr;
	}
}
