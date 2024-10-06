#include "CSFunctionMetaData.h"

#include "TypeGenerator/Register/CSMetaDataUtils.h"

void FCSFunctionMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FCSMemberMetaData::SerializeFromJson(JsonObject);

	const TArray<TSharedPtr<FJsonValue>>* ParametersArrayField;
	if (JsonObject->TryGetArrayField(TEXT("Parameters"), ParametersArrayField))
	{
		FCSMetaDataUtils::SerializeProperties(*ParametersArrayField, Parameters);
	}

	const TSharedPtr<FJsonObject>* ReturnValueObject;
	if (JsonObject->TryGetObjectField(TEXT("ReturnValue"), ReturnValueObject))
	{
		FCSMetaDataUtils::SerializeProperty(*ReturnValueObject, ReturnValue);
		
		//Since the return value has no name in the C# reflection. Just assign "ReturnValue" to it.
		ReturnValue.Name = "ReturnValue";
	}

	JsonObject->TryGetBoolField(TEXT("IsVirtual"), IsVirtual);
	FunctionFlags = FCSMetaDataUtils::GetFlags<EFunctionFlags>(JsonObject,"FunctionFlags");
}
