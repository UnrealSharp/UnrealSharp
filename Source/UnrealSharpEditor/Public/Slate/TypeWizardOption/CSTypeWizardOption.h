#pragma once

#include "CoreMinimal.h"
#include "CSScriptBuilder.h"

class FCSTypeWizardOption;

enum class ECSTypeConflictScope : uint8
{
	Class,
	ScriptStruct,
	Enum
};

struct FCSNewTypeParams
{
	TSharedPtr<const FCSTypeWizardOption> TypeOption;
	FString TypeName;
	UClass* ParentClass = nullptr;
	FString Namespace;
	FString OutputDirectory;
};

class FCSTypeWizardOption : public TSharedFromThis<FCSTypeWizardOption>
{
public:
	virtual ~FCSTypeWizardOption() = default;

	virtual FName GetOptionName() const = 0;
	virtual FText GetDisplayName() const = 0;
	virtual FText GetNameHint() const = 0;
	virtual int32 GetSortPriority() const { return 100; }

	virtual FString GetPrefix(const UClass* ParentClass) const = 0;
	virtual bool RequiresParentClass() const { return false; }
	virtual ECSTypeConflictScope GetConflictScope() const { return ECSTypeConflictScope::Class; }

	virtual void CollectUsings(const FCSNewTypeParams& Params, TArray<FString>& OutUsings) const = 0;
	virtual void AppendDeclaration(const FCSNewTypeParams& Params, FCSScriptBuilder& ScriptBuilder) const = 0;
	virtual void AppendBody(const FCSNewTypeParams& Params, FCSScriptBuilder& ScriptBuilder) const {}
	virtual FText GetDeclarationPreview(const FCSNewTypeParams& Params) const = 0;
	virtual const UField* FindConflictingType(const FString& TypeName, const FString& ManagedTypeName) const;

	FString GetManagedTypeName(const FString& TypeName, const UClass* ParentClass) const;
	FString GetManagedTypeName(const FCSNewTypeParams& Params) const;
	static FString GetFileName(const FCSNewTypeParams& Params);
};
