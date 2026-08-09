#include "Slate/TypeWizardOption/CSTypeWizardOption.h"
#include "Components/ActorComponent.h"
#include "GameFramework/Actor.h"
#include "UObject/UObjectIterator.h"
#include "Utilities/CSClassUtilities.h"

#define LOCTEXT_NAMESPACE "UnrealSharpTypeOption"

FString FCSTypeWizardOption::GetManagedTypeName(const FString& TypeName, const UClass* ParentClass) const
{
	const FString Prefix = GetPrefix(ParentClass);

	if (Prefix.IsEmpty() || TypeName.IsEmpty())
	{
		return TypeName;
	}

	if (TypeName.StartsWith(Prefix) && TypeName.Len() > Prefix.Len() && FChar::IsUpper(TypeName[Prefix.Len()]))
	{
		return TypeName;
	}

	return Prefix + TypeName;
}

FString FCSTypeWizardOption::GetManagedTypeName(const FCSNewTypeParams& Params) const
{
	return GetManagedTypeName(Params.TypeName, Params.ParentClass);
}

FString FCSTypeWizardOption::GetFileName(const FCSNewTypeParams& Params)
{
	return Params.TypeName + TEXT(".cs");
}

const UField* FCSTypeWizardOption::FindConflictingType(const FString& TypeName, const FString& ManagedTypeName) const
{
	auto Matches = [&TypeName, &ManagedTypeName](const UField* Field)
	{
		const FString FieldName = Field->GetName();
		return FieldName == TypeName || FieldName == ManagedTypeName;
	};

	switch (GetConflictScope())
	{
	case ECSTypeConflictScope::Enum:
		for (TObjectIterator<UEnum> It; It; ++It)
		{
			if (Matches(*It))
			{
				return *It;
			}
		}
		return nullptr;

	case ECSTypeConflictScope::ScriptStruct:
		for (TObjectIterator<UScriptStruct> It; It; ++It)
		{
			if (Matches(*It))
			{
				return *It;
			}
		}
		return nullptr;

	default:
		for (TObjectIterator<UClass> It; It; ++It)
		{
			const UClass* Class = *It;

			if (FCSClassUtilities::IsBlueprintObject(Class) || !Matches(Class))
			{
				continue;
			}

			return Class;
		}

		break;
	}
	
	return nullptr;
}

#undef LOCTEXT_NAMESPACE
