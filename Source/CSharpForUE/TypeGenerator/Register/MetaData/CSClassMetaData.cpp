#include "CSClassMetaData.h"

#include "TypeGenerator/Register/CSMetaDataUtils.h"

void FCSClassMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FCSTypeReferenceMetaData::SerializeFromJson(JsonObject);

	ClassFlags = FCSMetaDataUtils::GetFlags<EClassFlags>(JsonObject,"ClassFlags");
	
	ParentClass.SerializeFromJson(JsonObject->GetObjectField(TEXT("ParentClass")));

	FString ClassConfigNameStr;
	if (JsonObject->TryGetStringField(TEXT("ConfigCategory"), ClassConfigNameStr))
	{
		ClassConfigName = *ClassConfigNameStr;
	}

	TArray<FString> InterfacesStr;
	if (JsonObject->TryGetStringArrayField(TEXT("Interfaces"), InterfacesStr))
	{
		for (const FString& Interface : InterfacesStr)
		{
			Interfaces.Add(*Interface);
		}
	}

	const TArray<TSharedPtr<FJsonValue>>* FoundFunctions;
	if (JsonObject->TryGetArrayField(TEXT("Functions"), FoundFunctions))
	{
		FCSMetaDataUtils::SerializeFunctions(*FoundFunctions, Functions);
	}
	
	const TArray<TSharedPtr<FJsonValue>>* FoundVirtualFunctions;
	if (JsonObject->TryGetArrayField(TEXT("VirtualFunctions"), FoundVirtualFunctions))
	{
		for (const TSharedPtr<FJsonValue>& VirtualFunction : *FoundVirtualFunctions)
		{
			VirtualFunctions.Add(*VirtualFunction->AsObject()->GetStringField(TEXT("Name")));
		}
	}

	const TArray<TSharedPtr<FJsonValue>>* FoundProperties;
	if (JsonObject->TryGetArrayField(TEXT("Properties"), FoundProperties))
	{
		FCSMetaDataUtils::SerializeProperties(*FoundProperties, Properties);
	}
}
