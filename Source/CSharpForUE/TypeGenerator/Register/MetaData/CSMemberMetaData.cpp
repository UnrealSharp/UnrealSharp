#include "CSMemberMetaData.h"
#include "TypeGenerator/Register/CSMetaDataUtils.h"

void FCSMemberMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	Name = *JsonObject->GetStringField(TEXT("Name"));
	FCSMetaDataUtils::SerializeFromJson(JsonObject, MetaData);
}
