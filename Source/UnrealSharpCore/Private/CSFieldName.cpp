#include "CSFieldName.h"

#include "CSManager.h"
#include "UnrealSharpUtils.h"
#include "Json/CSJsonMacros.h"
#include "Json/CSJsonUtilities.h"
#include "Utilities/CSClassUtilities.h"

FCSFieldName FCSFieldName::FromNativeBase(const UField* NativeField)
{
	if (const UClass* Class = Cast<UClass>(NativeField))
	{
		NativeField = FCSClassUtilities::GetFirstNativeClass(Class);
	}

	FCSFieldName Result;
	Result.SourceName = *(FCSUnrealSharpUtils::GetPrefix(NativeField) + NativeField->GetName());
	Result.Namespace = FCSUnrealSharpUtils::GetNamespace(NativeField);
	Result.EngineName = NativeField->GetFName();
	return Result;
}

UCSManagedAssembly* FCSFieldName::ResolveAssembly() const
{
	UCSManagedAssembly* Assembly = UCSManager::Get().FindOrLoadAssembly(AssemblyName);
	check(::IsValid(Assembly));
	return Assembly;
}

UField* FCSFieldName::ResolveField() const
{
	UCSManagedAssembly* Assembly = ResolveAssembly();
	return Assembly->ResolveUField(*this);
}

void FCSFieldName::AppendFullName(FStringBuilderBase& Builder) const
{
	if (SourceName.IsNone())
	{
		return;
	}

	const FName NamespaceName = Namespace.GetFName();
	if (NamespaceName.IsNone())
	{
		return;
	}
	
	NamespaceName.AppendString(Builder);
	Builder.AppendChar(TEXT('.'));
	SourceName.AppendString(Builder);
}

bool FCSFieldName::Serialize(FConstObject JsonObject)
{
	START_JSON_SERIALIZE

	JSON_READ_STRING(SourceName, IS_REQUIRED);
	JSON_READ_STRING(EngineName, IS_REQUIRED);
	
	ECSFieldType FieldType;
	JSON_READ_ENUM(FieldType, IS_REQUIRED);
	
	if (FieldType != ECSFieldType::Unknown)
	{
		CALL_SERIALIZE(Namespace.Serialize(JsonObject));
		JSON_READ_STRING(AssemblyName, IS_REQUIRED);
	}

	END_JSON_SERIALIZE
}
