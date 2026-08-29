#include "CSFieldName.h"

#include "UnrealSharpUtils.h"
#include "Json/CSJsonMacros.h"
#include "Json/CSJsonUtilities.h"
#include "Utilities/CSClassUtilities.h"

FCSFieldName::FCSFieldName(const UField* NativeField)
{
	if (const UClass* Class = Cast<UClass>(NativeField))
	{
		NativeField = FCSClassUtilities::GetFirstNativeClass(Class);
	}
	
	Name = *(FCSUnrealSharpUtils::GetPrefix(NativeField) + NativeField->GetName());
	Namespace = FCSUnrealSharpUtils::GetNamespace(NativeField);
}

bool FCSFieldName::Serialize(FConstObject JsonObject)
{
	START_JSON_SERIALIZE
	JSON_READ_STRING(Name, IS_REQUIRED);
	CALL_SERIALIZE(Namespace.Serialize(JsonObject));	
	END_JSON_SERIALIZE
}
