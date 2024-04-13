#include "CSharpGeneratorUtilities.h"
#include "GlueGeneratorModule.h"
#include "Misc/Paths.h"
#include "Misc/FileHelper.h"
#include "UObject/Class.h"
#include "UObject/Package.h"
#include "UObject/EnumProperty.h"
#include "UObject/TextProperty.h"
#include "UObject/CoreRedirects.h"
#include "UObject/PropertyPortFlags.h"
#include "UObject/UnrealType.h"
#include "CSScriptBuilder.h"
#include "CSTooltipParser.h"
#include "Interfaces/IPluginManager.h"
#include "Kismet/BlueprintAsyncActionBase.h"

const FName ScriptNameMetaDataKey = TEXT("ScriptName");
const FName ScriptNoExportMetaDataKey = TEXT("ScriptNoExport");
const FName ScriptMethodMetaDataKey = TEXT("ScriptMethod");
const FName ScriptMethodSelfReturnMetaDataKey = TEXT("ScriptMethodSelfReturn");
const FName ScriptOperatorMetaDataKey = TEXT("ScriptOperator");
const FName ScriptConstantMetaDataKey = TEXT("ScriptConstant");
const FName ScriptConstantHostMetaDataKey = TEXT("ScriptConstantHost");
const FName BlueprintTypeMetaDataKey = TEXT("BlueprintType");
const FName NotBlueprintTypeMetaDataKey = TEXT("NotBlueprintType");
const FName BlueprintSpawnableComponentMetaDataKey = TEXT("BlueprintSpawnableComponent");
const FName BlueprintGetterMetaDataKey = TEXT("BlueprintGetter");
const FName BlueprintSetterMetaDataKey = TEXT("BlueprintSetter");
const FName DeprecatedPropertyMetaDataKey = TEXT("DeprecatedProperty");
const FName DeprecatedFunctionMetaDataKey = TEXT("DeprecatedFunction");
const FName DeprecationMessageMetaDataKey = TEXT("DeprecationMessage");
const FName CustomStructureParamMetaDataKey = TEXT("CustomStructureParam");
const FName HasNativeMakeMetaDataKey = TEXT("HasNativeMake");
const FName HasNativeBreakMetaDataKey = TEXT("HasNativeBreak");
const FName NativeBreakFuncMetaDataKey = TEXT("NativeBreakFunc");
const FName NativeMakeFuncMetaDataKey = TEXT("NativeMakeFunc");
const FName ReturnValueKey = TEXT("ReturnValue");
const TCHAR* HiddenMetaDataKey = TEXT("Hidden");

namespace ScriptGeneratorUtilities
{
	void AppendTooltip(const FProperty* Property, FCSScriptBuilder& Builder)
	{
		FCSTooltipParser::MakeCSharpTooltip(Builder, Property->GetToolTipText());
	}

	void AppendTooltip(const UField* Function, FCSScriptBuilder& Builder)
	{
		FCSTooltipParser::MakeCSharpTooltip(Builder, Function->GetToolTipText());
	}

	void AppendTooltip(const FText& ToolTip, FCSScriptBuilder& Builder)
	{
		FCSTooltipParser::MakeCSharpTooltip(Builder, ToolTip);
	}

	bool IsEnumValueValidWithoutPrefix(FString& RawName, const FString& Prefix)
	{
		if (RawName.Len() <= Prefix.Len())
		{
			return false;
		}
		TCHAR Name = RawName[Prefix.Len()];
		return FChar::IsAlpha(Name) || Name == '_';
	}
	
	bool IsBlueprintExposedClass(const UClass* InClass)
	{
		for (const UClass* ParentClass = InClass; ParentClass; ParentClass = ParentClass->GetSuperClass())
		{
			if (ParentClass->GetBoolMetaData(BlueprintTypeMetaDataKey) || ParentClass->HasMetaData(BlueprintSpawnableComponentMetaDataKey))
			{
				return true;
			}

			if (ParentClass->IsChildOf(UBlueprintFunctionLibrary::StaticClass()) || ParentClass->IsChildOf(UDeveloperSettings::StaticClass()))
			{
				return true;
			}

			if (ParentClass->GetBoolMetaData(NotBlueprintTypeMetaDataKey))
			{
				return false;
			}
		}

		return false;
	}

	void AddCheckObjectForValidity(FCSScriptBuilder& Builder)
	{
		Builder.AppendLine(TEXT("CheckObjectForValidity();"));
	}

	bool IsBlueprintExposedStruct(const UScriptStruct* InStruct)
	{
		for (const UScriptStruct* ParentStruct = InStruct; ParentStruct; ParentStruct = Cast<UScriptStruct>(ParentStruct->GetSuperStruct()))
		{
			if (ParentStruct->GetBoolMetaData(BlueprintTypeMetaDataKey))
			{
				return true;
			}

			if (ParentStruct->GetBoolMetaData(NotBlueprintTypeMetaDataKey))
			{
				return false;
			}
		}

		return false;
	}

	bool IsBlueprintExposedEnum(const UEnum* InEnum)
	{
		if (InEnum->GetBoolMetaData(BlueprintTypeMetaDataKey))
		{
			return true;
		}

		if (InEnum->GetBoolMetaData(NotBlueprintTypeMetaDataKey))
		{
			return false;
		}

		return false;
	}

	bool IsBlueprintExposedEnumEntry(const UEnum* InEnum, int32 InEnumEntryIndex)
	{
		return !InEnum->HasMetaData(HiddenMetaDataKey, InEnumEntryIndex);
	}

	bool IsBlueprintExposedProperty(const FProperty* InProp)
	{
		return InProp->HasAnyPropertyFlags(CPF_BlueprintVisible | CPF_BlueprintAssignable);
	}

	bool IsBlueprintExposedFunction(const UFunction* InFunc)
	{
		return InFunc->HasAnyFunctionFlags(FUNC_BlueprintCallable | FUNC_BlueprintEvent)
			&& !InFunc->HasMetaData(BlueprintGetterMetaDataKey)
			&& !InFunc->HasMetaData(BlueprintSetterMetaDataKey)
			&& !InFunc->HasMetaData(CustomStructureParamMetaDataKey)
			&& !InFunc->HasMetaData(NativeBreakFuncMetaDataKey)
			&& !InFunc->HasMetaData(NativeMakeFuncMetaDataKey);
	}

	bool IsBlueprintExposedField(const FField* InField)
	{
		if (const FProperty* Prop = Cast<const FProperty>(InField))
		{
			return IsBlueprintExposedProperty(Prop);
		}

		return false;
	}

	bool HasBlueprintExposedFields(const UStruct* InStruct)
	{
		for (TFieldIterator<const FField> FieldIt(InStruct); FieldIt; ++FieldIt)
		{
			if (IsBlueprintExposedField(*FieldIt))
			{
				return true;
			}
		}

		return false;
	}

	bool IsDeprecatedClass(const UClass* InClass, FString* OutDeprecationMessage)
	{
		if (InClass->HasAnyClassFlags(CLASS_Deprecated))
		{
			if (OutDeprecationMessage)
			{
				*OutDeprecationMessage = InClass->GetMetaData(DeprecationMessageMetaDataKey);
				if (OutDeprecationMessage->IsEmpty())
				{
					*OutDeprecationMessage = FString::Printf(TEXT("Class '%s' is deprecated."), *InClass->GetName());
				}
			}

			return true;
		}

		return false;
	}

	bool IsDeprecatedProperty(const FProperty* InProp, FString* OutDeprecationMessage)
	{
		if (InProp->HasMetaData(DeprecatedPropertyMetaDataKey))
		{
			if (OutDeprecationMessage)
			{
				*OutDeprecationMessage = InProp->GetMetaData(DeprecationMessageMetaDataKey);
				if (OutDeprecationMessage->IsEmpty())
				{
					*OutDeprecationMessage = FString::Printf(TEXT("Property '%s' is deprecated."), *InProp->GetName());
				}
			}

			return true;
		}

		return false;
	}

	bool IsDeprecatedFunction(const UFunction* InFunc, FString* OutDeprecationMessage)
	{
		if (InFunc->HasMetaData(DeprecatedFunctionMetaDataKey))
		{
			if (OutDeprecationMessage)
			{
				*OutDeprecationMessage = InFunc->GetMetaData(DeprecationMessageMetaDataKey);
				if (OutDeprecationMessage->IsEmpty())
				{
					*OutDeprecationMessage = FString::Printf(TEXT("Function '%s' is deprecated."), *InFunc->GetName());
				}
			}

			return true;
		}

		return false;
	}

	bool ShouldExportClass(const UClass* InClass)
	{
		return IsBlueprintExposedClass(InClass)
			|| HasBlueprintExposedFields(InClass)
			|| InClass->IsChildOf(AActor::StaticClass())
			|| InClass->IsChildOf(USubsystem::StaticClass())
		    || InClass->IsChildOf(UBlueprintAsyncActionBase::StaticClass());
	}

	bool ShouldExportStruct(const UScriptStruct* InStruct)
	{
		return IsBlueprintExposedStruct(InStruct) || HasBlueprintExposedFields(InStruct);
	}

	bool ShouldExportEnum(const UEnum* InEnum)
	{
		return IsBlueprintExposedEnum(InEnum);
	}

	bool ShouldExportEnumEntry(const UEnum* InEnum, int32 InEnumEntryIndex)
	{
		return IsBlueprintExposedEnumEntry(InEnum, InEnumEntryIndex);
	}

	bool ShouldExportProperty(const FProperty* InProp)
	{
		const bool bCanScriptExport = !InProp->HasMetaData(ScriptNoExportMetaDataKey);
		return bCanScriptExport && (IsBlueprintExposedProperty(InProp) || IsDeprecatedProperty(InProp) || InProp->HasAnyPropertyFlags(CPF_NativeAccessSpecifierPublic));
	}

	bool ShouldExportEditorOnlyProperty(const FProperty* InProp)
	{
		const bool bCanScriptExport = !InProp->HasMetaData(ScriptNoExportMetaDataKey);
		return bCanScriptExport && GIsEditor && (InProp->HasAnyPropertyFlags(CPF_Edit) || IsDeprecatedProperty(InProp));
	}

	bool ShouldExportFunction(const UFunction* InFunc)
	{
		if (InFunc->HasMetaData(ScriptNoExportMetaDataKey))
		{
			return false;
		}
		
		return InFunc->HasMetaData(ScriptMethodMetaDataKey) || IsBlueprintExposedFunction(InFunc);
	}

	bool IsInterfaceFunction(UFunction* Function)
	{
		UClass* Class = Cast<UClass>(Function->GetOuter());

		if (!Class)
		{
			return false;
		}

		if (Class->HasAnyClassFlags(CLASS_Interface))
		{
			return true;
		}

		for (UClass* TempClass = Class; TempClass != nullptr && Function != nullptr; TempClass = TempClass->GetSuperClass())
		{
			for (const FImplementedInterface& I : TempClass->Interfaces)
			{
				if (FindUField<UFunction>(I.Class, Function->GetFName()))
				{
					return true;
				}
			}
		}

		return false;
	}

	FString StripPropertyPrefix(const FString& InName)
	{
		int32 NameOffset = 0;

		for (;;)
		{
			// Strip the "b" prefix from bool names
			if (InName.Len() - NameOffset >= 2 && InName[NameOffset] == TEXT('b') && FChar::IsUpper(InName[NameOffset + 1]))
			{
				NameOffset += 1;
				continue;
			}

			// Strip the "In" prefix from names
			if (InName.Len() - NameOffset >= 3 && InName[NameOffset] == TEXT('I') && InName[NameOffset + 1] == TEXT('n') && FChar::IsUpper(InName[NameOffset + 2]))
			{
				NameOffset += 2;
				continue;
			}
			break;
		}
		return NameOffset ? InName.RightChop(NameOffset) : InName;
	}

	FString FScriptNameMapper::ScriptifyName(const FString& InName, const EScriptNameKind InNameKind) const
	{
		switch (InNameKind)
		{
		case EScriptNameKind::Property:
		case EScriptNameKind::Parameter:
			return StripPropertyPrefix(InName);
		default:;
		}
		return InName;
	}

	FString FScriptNameMapper::ScriptifyName(const FString& InName, const EScriptNameKind InNameKind, const TSet<FString>& ReservedNames) const
	{
		switch (InNameKind)
		{
		case EScriptNameKind::Property:
		case EScriptNameKind::Parameter:
		{
			// Only strip prefixes from the name if it doesn't create a name collision
			FString StrippedName = StripPropertyPrefix(InName);
			if (ReservedNames.Contains(StrippedName))
			{
				return InName;
			}
			return StrippedName;
		}
		default: ;
		}
		return InName;
	}

	FString FScriptNameMapper::GetFieldModule(const FField* InField) const
	{
		UPackage* ScriptPackage = InField->GetOutermost();
		
		const FString PackageName = ScriptPackage->GetName();
		if (PackageName.StartsWith(TEXT("/Script/")))
		{
			return PackageName.RightChop(8); // Chop "/Script/" from the name
		}

		check(PackageName[0] == TEXT('/'));
		int32 RootNameEnd = 1;
		for (; PackageName[RootNameEnd] != TEXT('/'); ++RootNameEnd) {}
		return PackageName.Mid(1, RootNameEnd - 1);
	}

	FString FScriptNameMapper::GetFieldPlugin(const FField* InField) const
	{
		static const TMap<FName, FString> ModuleNameToPluginMap = []()
		{
			IPluginManager& PluginManager = IPluginManager::Get();

			// Build up a map of plugin modules -> plugin names
			TMap<FName, FString> PluginModules;
			{
				TArray<TSharedRef<IPlugin>> Plugins = PluginManager.GetDiscoveredPlugins();
				for (const TSharedRef<IPlugin>& Plugin : Plugins)
				{
					for (const FModuleDescriptor& PluginModule : Plugin->GetDescriptor().Modules)
				{
					PluginModules.Add(PluginModule.Name, Plugin->GetName());
				}
			}
		}
		return PluginModules;
	}();

	const FString* FieldPluginNamePtr = ModuleNameToPluginMap.Find(*GetFieldModule(InField));
	return FieldPluginNamePtr ? *FieldPluginNamePtr : FString();
}

bool GetFieldScriptNameFromMetaDataImpl(const FField* InField, const FName InMetaDataKey, FString& OutFieldName)
{
	// See if we have a name override in the meta-data
	if (!InMetaDataKey.IsNone())
	{
		OutFieldName = InField->GetMetaData(InMetaDataKey);

		// This may be a semi-colon separated list - the first item is the one we want for the current name
		if (!OutFieldName.IsEmpty())
		{
			int32 SemiColonIndex = INDEX_NONE;
			if (OutFieldName.FindChar(TEXT(';'), SemiColonIndex))
			{
				OutFieldName.RemoveAt(SemiColonIndex, OutFieldName.Len() - SemiColonIndex, /*bAllowShrinking*/false);
			}

			return true;
		}
	}

	return false;
}

bool GetStructScriptNameFromMetaDataImpl(const UStruct* InStruct, const FName InMetaDataKey, FString& OutFieldName)
{
	// See if we have a name override in the meta-data
	if (!InMetaDataKey.IsNone())
	{
		OutFieldName = InStruct->GetMetaData(InMetaDataKey);

		// This may be a semi-colon separated list - the first item is the one we want for the current name
		if (!OutFieldName.IsEmpty())
		{
			int32 SemiColonIndex = INDEX_NONE;
			if (OutFieldName.FindChar(TEXT(';'), SemiColonIndex))
			{
				OutFieldName.RemoveAt(SemiColonIndex, OutFieldName.Len() - SemiColonIndex, /*bAllowShrinking*/false);
			}

			return true;
		}
	}

	return false;
}

bool GetDeprecatedFieldScriptNamesFromMetaDataImpl(const void* InField, const FName InMetaDataKey, TArray<FString>& OutFieldNames)
{
	// See if we have a name override in the meta-data
	if (!InMetaDataKey.IsNone())
	{
		FString FieldName;
	
		if (const UStruct* Struct = static_cast<const UStruct*>(InField))
		{
			FieldName = Struct->GetMetaData(InMetaDataKey);
		}
		else
		{
			const FField* Field = static_cast<const FField*>(InField);
			FieldName = Field->GetMetaData(InMetaDataKey);
		}

		// This may be a semi-colon separated list - everything but the first item is deprecated
		if (!FieldName.IsEmpty())
		{
			FieldName.ParseIntoArray(OutFieldNames, TEXT(";"), false);

			// Remove the non-deprecated entry
			if (OutFieldNames.Num() > 0)
			{
				OutFieldNames.RemoveAt(0, 1, /*bAllowShrinking*/false);
			}

			// Trim whitespace and remove empty items
			OutFieldNames.RemoveAll([](FString& InStr)
			{
				InStr.TrimStartAndEndInline();
				return InStr.IsEmpty();
			});

			return true;
		}
	}

	return false;
}

FString GetFieldScriptNameImpl(const FProperty* InField, const FName InMetaDataKey)
{
	FString FieldName;

	// First see if we have a name override in the meta-data
	if (GetFieldScriptNameFromMetaDataImpl(InField, InMetaDataKey, FieldName))
	{
		return FieldName;
	}

	// Just use the field name if we have no meta-data
	if (FieldName.IsEmpty())
	{
		FieldName = InField->GetName();

		// Strip the "E" prefix from enum names
		if (InField->IsA<FEnumProperty>() && FieldName.Len() >= 2 && FieldName[0] == TEXT('E') && FChar::IsUpper(FieldName[1]))
		{
			FieldName.RemoveAt(0, 1, /*bAllowShrinking*/false);
		}
	}

	return FieldName;
}

FString GetFieldScriptNameImpl(const UStruct* InField, const FName InMetaDataKey)
{
	FString FieldName;
	GetStructScriptNameFromMetaDataImpl(InField, InMetaDataKey, FieldName);
		
	if (FieldName.IsEmpty())
	{
		FieldName = InField->GetName();
	}

	// Remove '-'s and spaces from the class names.
	FieldName.ReplaceCharInline(TCHAR('-'), TCHAR(' '));
	FieldName.RemoveSpacesInline();

	if (InField->IsChildOf<UInterface>())
	{
		FieldName = FString::Printf(TEXT("I%s"), *FieldName);
	}

	return FieldName;
}

TArray<FString> GetDeprecatedFieldScriptNamesImpl(const FField* InField, const FName InMetaDataKey)
{
	TArray<FString> FieldNames;

	// First see if we have a name override in the meta-data
	if (GetDeprecatedFieldScriptNamesFromMetaDataImpl(InField, InMetaDataKey, FieldNames))
	{
		return FieldNames;
	}
	
	const FCoreRedirectObjectName CurrentName = InField->GetName();
	TArray<FCoreRedirectObjectName> PreviousNames;
	FCoreRedirects::FindPreviousNames(ECoreRedirectFlags::Type_Property, CurrentName, PreviousNames);

	FieldNames.Reserve(PreviousNames.Num());
	for (const FCoreRedirectObjectName& PreviousName : PreviousNames)
	{
		// Redirects can be used to redirect outers
		// We want to skip those redirects as we only care about changes within the current scope
		if (!PreviousName.OuterName.IsNone() && PreviousName.OuterName != CurrentName.OuterName)
		{
			continue;
		}

		// Redirects can often keep the same name when updating the path
		// We want to skip those redirects as we only care about name changes
		if (PreviousName.ObjectName == CurrentName.ObjectName)
		{
			continue;
		}
		
		FString FieldName = PreviousName.ObjectName.ToString();

		// Strip the "E" prefix from enum names
		if (InField->IsA<FEnumProperty>() && FieldName.Len() >= 2 && FieldName[0] == TEXT('E') && FChar::IsUpper(FieldName[1]))
		{
			FieldName.RemoveAt(0, 1, /*bAllowShrinking*/false);
		}

		FieldNames.Add(MoveTemp(FieldName));
	}

	return FieldNames;
}

TArray<FString> GetDeprecatedFieldScriptNamesImpl(const UStruct* InField, const FName InMetaDataKey)
{
	TArray<FString> FieldNames;

	// First see if we have a name override in the meta-data
	if (GetDeprecatedFieldScriptNamesFromMetaDataImpl(InField, InMetaDataKey, FieldNames))
	{
		return FieldNames;
	}

	// Just use the redirects if we have no meta-data
	ECoreRedirectFlags RedirectFlags = ECoreRedirectFlags::None;
	if (InField->IsA<UFunction>())
	{
		RedirectFlags = ECoreRedirectFlags::Type_Function;
	}
	else if (InField->IsA<UClass>())
	{
		RedirectFlags = ECoreRedirectFlags::Type_Class;
	}
	else if (InField->IsA<UScriptStruct>())
	{
		RedirectFlags = ECoreRedirectFlags::Type_Struct;
	}
	else if (InField->IsA<UEnum>())
	{
		RedirectFlags = ECoreRedirectFlags::Type_Enum;
	}
	
	const FCoreRedirectObjectName CurrentName = FCoreRedirectObjectName(InField);
	TArray<FCoreRedirectObjectName> PreviousNames;
	FCoreRedirects::FindPreviousNames(RedirectFlags, CurrentName, PreviousNames);

	FieldNames.Reserve(PreviousNames.Num());
	for (const FCoreRedirectObjectName& PreviousName : PreviousNames)
	{
		// Redirects can be used to redirect outers
		// We want to skip those redirects as we only care about changes within the current scope
		if (!PreviousName.OuterName.IsNone() && PreviousName.OuterName != CurrentName.OuterName)
		{
			continue;
		}

		// Redirects can often keep the same name when updating the path
		// We want to skip those redirects as we only care about name changes
		if (PreviousName.ObjectName == CurrentName.ObjectName)
		{
			continue;
		}
		
		FString FieldName = PreviousName.ObjectName.ToString();

		// Strip the "E" prefix from enum names
		if (InField->IsA<UEnum>() && FieldName.Len() >= 2 && FieldName[0] == TEXT('E') && FChar::IsUpper(FieldName[1]))
		{
			FieldName.RemoveAt(0, 1, /*bAllowShrinking*/false);
		}

		FieldNames.Add(MoveTemp(FieldName));
	}

	return FieldNames;
}


FString FScriptNameMapper::GetScriptClassName(const UClass* InClass) const
{
	return GetFieldScriptNameImpl(InClass, ScriptNameMetaDataKey);
}

TArray<FString> FScriptNameMapper::GetDeprecatedClassScriptNames(const UClass* InClass) const
{
	return GetDeprecatedFieldScriptNamesImpl(InClass, ScriptNameMetaDataKey);
}

FString FScriptNameMapper::GetStructScriptName(const UScriptStruct* InStruct) const
{
	return GetFieldScriptNameImpl(InStruct, ScriptNameMetaDataKey);
}

FString FScriptNameMapper::GetTypeScriptName(const UStruct* InType) const
{
	if (const UScriptStruct* ScriptStruct = Cast<UScriptStruct>(InType))
	{
		return GetStructScriptName(ScriptStruct);
	}
		
	const UClass* Class = CastChecked<UClass>(InType);
	return GetScriptClassName(Class);
}

TArray<FString> FScriptNameMapper::GetDeprecatedStructScriptNames(const UScriptStruct* InStruct) const
{
	return GetDeprecatedFieldScriptNamesImpl(InStruct, ScriptNameMetaDataKey);
}

FString FScriptNameMapper::MapEnumName(const FEnumProperty* InEnum) const
{
	return GetFieldScriptNameImpl(InEnum, ScriptNameMetaDataKey);
}

TArray<FString> FScriptNameMapper::GetDeprecatedEnumScriptNames(const FEnumProperty* InEnum) const
{
	return GetDeprecatedFieldScriptNamesImpl(InEnum, ScriptNameMetaDataKey);
}

FString FScriptNameMapper::MapEnumEntryName(const FEnumProperty* InEnum, const int32 InEntryIndex) const
{
	FString EnumEntryName;

	// First see if we have a name override in the meta-data
	{
		EnumEntryName = InEnum->GetMetaData("ScriptName");

		// This may be a semi-colon separated list - the first item is the one we want for the current name
		if (!EnumEntryName.IsEmpty())
		{
			int32 SemiColonIndex = INDEX_NONE;
			if (EnumEntryName.FindChar(TEXT(';'), SemiColonIndex))
			{
				EnumEntryName.RemoveAt(SemiColonIndex, EnumEntryName.Len() - SemiColonIndex, /*bAllowShrinking*/false);
			}
		}
	}
	
	// Just use the entry name if we have no meta-data
	if (EnumEntryName.IsEmpty())
	{
		EnumEntryName = InEnum->GetEnum()->GetNameStringByIndex(InEntryIndex);
	}

	return ScriptifyName(EnumEntryName, EScriptNameKind::Enum);
}

FString FScriptNameMapper::MapDelegateName(const UFunction* InDelegateSignature) const
{
	FString DelegateName = InDelegateSignature->GetName().LeftChop(19); // Trim the "__DelegateSignature" suffix from the name
	return ScriptifyName(DelegateName, EScriptNameKind::Function);
}

FString FScriptNameMapper::MapFunctionName(const UFunction* InFunc) const
{
	FString FuncName = GetFieldScriptNameImpl(InFunc, ScriptNameMetaDataKey);
	return ScriptifyName(FuncName, EScriptNameKind::Function);
}

TArray<FString> FScriptNameMapper::GetDeprecatedFunctionScriptNames(const UFunction* InFunc) const
{
	const UClass* FuncOwner = InFunc->GetOwnerClass();
	check(FuncOwner);

	TArray<FString> FuncNames = GetDeprecatedFieldScriptNamesImpl(InFunc, ScriptNameMetaDataKey);
	for (auto FuncNamesIt = FuncNames.CreateIterator(); FuncNamesIt; ++FuncNamesIt)
	{
		FString& FuncName = *FuncNamesIt;

		// Remove any deprecated names that clash with an existing Script exposed function
		const UFunction* DeprecatedFunc = FuncOwner->FindFunctionByName(*FuncName);
		if (DeprecatedFunc && ShouldExportFunction(DeprecatedFunc))
		{
			FuncNamesIt.RemoveCurrent();
			continue;
		}

		FuncName = ScriptifyName(FuncName, EScriptNameKind::Function);
	}

	return FuncNames;
}

FString FScriptNameMapper::MapScriptMethodName(const UFunction* InFunc) const
{
	FString ScriptMethodName;
	if (GetStructScriptNameFromMetaDataImpl(InFunc, ScriptMethodMetaDataKey, ScriptMethodName))
	{
		return ScriptifyName(ScriptMethodName, EScriptNameKind::ScriptMethod);
	}
	return MapFunctionName(InFunc);
}

TArray<FString> FScriptNameMapper::GetDeprecatedScriptMethodScriptNames(const UFunction* InFunc) const
{
	TArray<FString> ScriptMethodNames;
	if (GetDeprecatedFieldScriptNamesFromMetaDataImpl(InFunc, ScriptMethodMetaDataKey, ScriptMethodNames))
	{
		for (FString& ScriptMethodName : ScriptMethodNames)
		{
			ScriptMethodName = ScriptifyName(ScriptMethodName, EScriptNameKind::ScriptMethod);
		}
		return ScriptMethodNames;
	}
	return GetDeprecatedFunctionScriptNames(InFunc);
}

FString FScriptNameMapper::MapScriptConstantName(const UFunction* InFunc) const
{
	FString ScriptConstantName;
	if (!GetStructScriptNameFromMetaDataImpl(InFunc, ScriptConstantMetaDataKey, ScriptConstantName))
	{
		ScriptConstantName = GetFieldScriptNameImpl(InFunc, ScriptNameMetaDataKey);
	}
	return ScriptifyName(ScriptConstantName, EScriptNameKind::Constant);
}

TArray<FString> FScriptNameMapper::GetDeprecatedScriptConstantScriptNames(const UFunction* InFunc) const
{
	TArray<FString> ScriptConstantNames;
	
	if (!GetDeprecatedFieldScriptNamesFromMetaDataImpl(InFunc, ScriptConstantMetaDataKey, ScriptConstantNames))
	{
		ScriptConstantNames = GetDeprecatedFieldScriptNamesImpl(InFunc, ScriptNameMetaDataKey);
	}
	for (FString& ScriptConstantName : ScriptConstantNames)
	{
		ScriptConstantName = ScriptifyName(ScriptConstantName, EScriptNameKind::Constant);
	}
	return ScriptConstantNames;
}

FString FScriptNameMapper::MapPropertyName(const FProperty* InProp, const TSet<FString>& ReservedNames) const
{
	FString PropName = GetFieldScriptNameImpl(InProp, ScriptNameMetaDataKey);
	FString ScriptName = ScriptifyName(PropName, EScriptNameKind::Property, ReservedNames);

	if (ScriptName == InProp->Owner.GetName())
	{
		return "K2_" + ScriptName;
	}

	return ScriptName;
}

TArray<FString> FScriptNameMapper::GetDeprecatedPropertyScriptNames(const FProperty* InProp) const
{
	const UStruct* PropOwner = InProp->GetOwnerStruct();
	check(PropOwner);

	TArray<FString> PropNames = GetDeprecatedFieldScriptNamesImpl(InProp, ScriptNameMetaDataKey);
	for (auto PropNamesIt = PropNames.CreateIterator(); PropNamesIt; ++PropNamesIt)
	{
		FString& PropName = *PropNamesIt;

		// Remove any deprecated names that clash with an existing script exposed property
		const FProperty* DeprecatedProp = PropOwner->FindPropertyByName(*PropName);
		if (DeprecatedProp && ShouldExportProperty(DeprecatedProp))
		{
			PropNamesIt.RemoveCurrent();
			continue;
		}

		PropName = ScriptifyName(PropName, EScriptNameKind::Property);
	}

	return PropNames;
}

FString FScriptNameMapper::MapParameterName(const FProperty* InProp) const
{
	FString PropName = GetFieldScriptNameImpl(InProp, ScriptNameMetaDataKey);
	return ScriptifyName(PropName, EScriptNameKind::Parameter);
}

FString GetEnumValueMetaData(const UEnum& InEnum, const TCHAR* MetadataKey, int32 ValueIndex)
{
	FString EnumName = InEnum.GetNameStringByIndex(ValueIndex);
	FString EnumValueMetaDataKey(*FString::Printf(TEXT("%s.%s"), *EnumName, *EnumName));

	if (InEnum.HasMetaData(*EnumValueMetaDataKey, ValueIndex))
	{
		return InEnum.GetMetaData(*EnumValueMetaDataKey, ValueIndex);
	}
	return FString();
}

FString GetEnumValueToolTip(const UEnum& InEnum, int32 ValueIndex)
{
	// Mimic behavior of UEnum::GetToolTipText, which unfortunately is not available since script generator is not actually WITH_EDITOR
	FString LocalizedToolTip;
	const FString NativeToolTip = GetEnumValueMetaData(InEnum, *NAME_ToolTip.ToString(), ValueIndex);

	FString Namespace = TEXT("UObjectToolTips");
	FString Key = ValueIndex == INDEX_NONE
		? InEnum.GetFullGroupName(true) + TEXT(".") + InEnum.GetName()
		: InEnum.GetFullGroupName(true) + TEXT(".") + InEnum.GetName() + TEXT(".") + InEnum.GetNameStringByIndex(ValueIndex);

	return LocalizedToolTip;
}

bool IsBlueprintFunctionLibrary(const UClass* InClass)
{
	UClass* SuperClass = InClass->GetSuperClass();

	while (SuperClass != nullptr)
	{
		if (SuperClass->GetClass() == UBlueprintFunctionLibrary::StaticClass())
		{
			return true;
		}

		SuperClass = SuperClass->GetSuperClass();
	}

	return false;
}
	
}
