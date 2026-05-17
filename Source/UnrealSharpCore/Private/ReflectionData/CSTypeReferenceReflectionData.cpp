#include "ReflectionData/CSTypeReferenceReflectionData.h"
#include "CSManager.h"
#include "Json/CSJsonMacros.h"
#include "Json/CSJsonUtilities.h"

bool FCSMetaDataEntry::Serialize(FConstObject JsonObject)
{
	START_JSON_SERIALIZE
	
	JSON_READ_STRING(Key, IS_REQUIRED);
	JSON_READ_STRING(Value, IS_OPTIONAL);
	
	END_JSON_SERIALIZE
}

void FCSTypeReferenceReflectionData::SerializeFromJsonString(TCHAR* RawJsonString)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(FCSTypeReferenceReflectionData::StartSerializeFromJson);
	
	RawReflectionData = RawJsonString;
	
	FDocument ParsedDocument;
	if (!ParseJsonString(RawJsonString, ParsedDocument))
	{
		UE_LOGFMT(LogUnrealSharp, Fatal, "Failed to parse JSON reflection data for type {0}. Check logs for meta data failing to parse.", *FieldName.GetFullName().ToString());
	}
	
	TOptional<FConstObject> RootObject = GetRootObject(ParsedDocument);
	if (!Serialize(RootObject.GetValue()))
	{
		UE_LOGFMT(LogUnrealSharp, Fatal, "Failed to parse JSON reflection data for type {0}. Check logs for meta data failing to parse.", *FieldName.GetFullName().ToString());
	}
}

bool FCSTypeReferenceReflectionData::Serialize(FConstObject JsonObject)
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
