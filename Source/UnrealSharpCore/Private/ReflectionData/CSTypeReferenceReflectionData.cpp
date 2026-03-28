#include "ReflectionData/CSTypeReferenceReflectionData.h"
#include "ReflectionData/CSReflectionDataBase.h"
#include "CSManager.h"


bool FCSMetaDataEntry::Serialize(TSharedPtr<FJsonObject> JsonObject)
{
	START_JSON_SERIALIZE
	
	JSON_READ_STRING(Key, IS_REQUIRED);
	JSON_READ_STRING(Value, IS_OPTIONAL);
	
	END_JSON_SERIALIZE
}

void FCSTypeReferenceReflectionData::SerializeFromJsonString(const char* RawJsonString)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(FCSTypeReferenceReflectionData::StartSerializeFromJson);
	
	if (!RawReflectionData.IsEmpty())
	{
		UE_LOGFMT(LogUnrealSharp, Fatal, "Attempted to re-serialize reflection data for type {0}", *FieldName.GetFullName().ToString());
	}
	
	RawReflectionData = RawJsonString;
	
	TSharedRef<TJsonReader<>> JsonReader = TJsonReaderFactory<>::Create(RawReflectionData);
		
	TSharedPtr<FJsonObject> JsonReflectionData;
	if (!FJsonSerializer::Deserialize(JsonReader, JsonReflectionData))
	{
		UE_LOGFMT(LogUnrealSharp, Fatal, "Failed to deserialize reflection data JSON for type {0}", *FieldName.GetFullName().ToString());
	}

	if (Serialize(JsonReflectionData))
	{
		return;
	}
	
	UE_LOGFMT(LogUnrealSharp, Fatal, "Failed to parse JSON reflection data for type {0}. Check logs for meta data failing to parse.", *FieldName.GetFullName().ToString());
}

bool FCSTypeReferenceReflectionData::Serialize(TSharedPtr<FJsonObject> JsonObject)
{
	START_JSON_SERIALIZE
	
	JSON_READ_STRING(AssemblyName, IS_REQUIRED);
	CALL_SERIALIZE(FieldName.Serialize(JsonObject));
	JSON_PARSE_OBJECT_ARRAY(SourceGeneratorDependencies, IS_OPTIONAL);
	JSON_PARSE_OBJECT_ARRAY(MetaData, IS_OPTIONAL);

	END_JSON_SERIALIZE
}

UCSManagedAssembly* FCSTypeReferenceReflectionData::GetDefinitionFieldAssembly() const
{
	UCSManagedAssembly* Assembly = UCSManager::Get().FindOrLoadAssembly(AssemblyName);
	check(::IsValid(Assembly));
	return Assembly;
}

UField* FCSTypeReferenceReflectionData::ResolveUField() const
{
	UCSManagedAssembly* Assembly = GetDefinitionFieldAssembly();
	return Assembly->ResolveUField(FieldName);
}

UPackage* FCSTypeReferenceReflectionData::GetDefinitionFieldPackage() const
{
	return UCSManager::Get().GetPackage(FieldName.GetNamespace());
}
