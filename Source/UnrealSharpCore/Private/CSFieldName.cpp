#include "CSFieldName.h"

#include "UnrealSharpUtils.h"
#include "Utilities/CSClassUtilities.h"

FCSFieldName::FCSFieldName(UField* NativeField)
{
	if (UClass* Class = Cast<UClass>(NativeField))
	{
		NativeField = FCSClassUtilities::GetFirstNativeClass(Class);
	}
	
	Name = NativeField->GetFName();
	Namespace = FCSUnrealSharpUtils::GetNamespace(NativeField);
}

bool FCSFieldName::Serialize(TSharedPtr<FJsonObject> JsonObject)
{
	START_JSON_SERIALIZE
	
	JSON_READ_STRING(Name, IS_REQUIRED);
	CALL_SERIALIZE(Namespace.Serialize(JsonObject));	
	
	END_JSON_SERIALIZE
}
