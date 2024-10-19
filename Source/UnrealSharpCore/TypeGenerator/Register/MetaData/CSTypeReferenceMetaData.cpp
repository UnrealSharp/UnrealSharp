#include "CSTypeReferenceMetaData.h"

#include "TypeGenerator/Register/CSMetaDataUtils.h"

void FCSTypeReferenceMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	Name = *JsonObject->GetStringField(TEXT("Name"));

	FString NamespaceStr;
	if (JsonObject->TryGetStringField(TEXT("Namespace"), NamespaceStr))
	{
		Namespace = *NamespaceStr;
	}

	FString AssemblyNameStr;
	if (JsonObject->TryGetStringField(TEXT("AssemblyName"), AssemblyNameStr))
	{
		AssemblyName = *AssemblyNameStr;
	}
	
	FCSMetaDataUtils::SerializeFromJson(JsonObject, MetaData);
}
