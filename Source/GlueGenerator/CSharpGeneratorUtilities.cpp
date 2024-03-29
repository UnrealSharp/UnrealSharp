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

// copy and paste from FTextLocalizationManager
// We can't use FTextLocalizationManager because the tooltip localization files are not loaded in
// the script generator. We could force them to load, but that requires a dubious UE4 mod, and I wasn't sure if it
// affected the output of the generated C++ files
//
// The localization file format is fairly stable (none of this serialization code in FTextLocalizationManager has changed)
// so for now we copy and paste, and load the tooltips localization ourselves.
namespace LocalizationHack
{
	const FGuid LocResMagic = FGuid(0x7574140E, 0xFC034A67, 0x9D90154A, 0x1B7F37C3);

	enum class ELocResVersion : uint8
	{
		/** Legacy format file - will be missing the magic number. */
		Legacy = 0,
		/** Compact format file - strings are stored in a LUT to avoid duplication. */
		Compact,

		LatestPlusOne,
		Latest = LatestPlusOne - 1
	};

	struct FLocalizationEntryTracker
	{
		struct FEntry
		{
			FString LocResID;
			uint32 SourceStringHash;
			FString LocalizedString;
		};

		typedef TArray<FEntry> FEntryArray;
		typedef TMap<FString, FEntryArray, FDefaultSetAllocator, FLocKeyMapFuncs<FEntryArray>> FKeysTable;
		typedef TMap<FString, FKeysTable, FDefaultSetAllocator, FLocKeyMapFuncs<FKeysTable>> FNamespacesTable;

		FNamespacesTable Namespaces;

		void LoadFromDirectory(const FString& DirectoryPath);
		bool LoadFromFile(const FString& FilePath);
		bool LoadFromArchive(FArchive& Archive, const FString& Identifier);
	};

	void FLocalizationEntryTracker::LoadFromDirectory(const FString& DirectoryPath)
	{
		// Find resources in the specified folder.
		TArray<FString> ResourceFileNames;
		IFileManager::Get().FindFiles(ResourceFileNames, *(DirectoryPath / TEXT("*.locres")), true, false);

		for (const FString& ResourceFileName : ResourceFileNames)
		{
			LoadFromFile(FPaths::ConvertRelativePathToFull(DirectoryPath / ResourceFileName));
		}
	}

	bool FLocalizationEntryTracker::LoadFromFile(const FString& FilePath)
	{
		TUniquePtr<FArchive> Reader(IFileManager::Get().CreateFileReader(*FilePath));
		if (!Reader)
		{
			UE_LOG(LogGlueGenerator, Warning, TEXT("LocRes '%s' could not be opened for reading!"), *FilePath);
			return false;
		}

		bool Success = LoadFromArchive(*Reader, FilePath);
		Success &= Reader->Close();
		return Success;
	}

	bool FLocalizationEntryTracker::LoadFromArchive(FArchive& Archive, const FString& LocalizationResourceIdentifier)
	{
		Archive.SetForceUnicode(true);

		// Read magic number
		FGuid MagicNumber;

		if (Archive.TotalSize() >= sizeof(FGuid))
		{
			Archive << MagicNumber;
		}

		ELocResVersion VersionNumber = ELocResVersion::Legacy;
		if (MagicNumber == LocResMagic)
		{
			Archive << VersionNumber;
		}
		else
		{
			// Legacy LocRes files lack the magic number, assume that's what we're dealing with, and seek back to the start of the file
			Archive.Seek(0);
			//UE_LOG(LogTextLocalizationResource, Warning, TEXT("LocRes '%s' failed the magic number check! Assuming this is a legacy resource (please re-generate your localization resources!)"), *LocalizationResourceIdentifier);
			UE_LOG(LogGlueGenerator, Log, TEXT("LocRes '%s' failed the magic number check! Assuming this is a legacy resource (please re-generate your localization resources!)"), *LocalizationResourceIdentifier);
		}

		// Read the localized string array
		TArray<FString> LocalizedStringArray;
		if (VersionNumber >= ELocResVersion::Compact)
		{
			int64 LocalizedStringArrayOffset = INDEX_NONE;
			Archive << LocalizedStringArrayOffset;

			if (LocalizedStringArrayOffset != INDEX_NONE)
			{
				const int64 CurrentFileOffset = Archive.Tell();
				Archive.Seek(LocalizedStringArrayOffset);
				Archive << LocalizedStringArray;
				Archive.Seek(CurrentFileOffset);
			}
		}

		// Read namespace count
		uint32 NamespaceCount;
		Archive << NamespaceCount;

		for (uint32 i = 0; i < NamespaceCount; ++i)
		{
			// Read namespace
			FString Namespace;
			Archive << Namespace;

			// Read key count
			uint32 KeyCount;
			Archive << KeyCount;

			FKeysTable& KeyTable = Namespaces.FindOrAdd(*Namespace);

			for (uint32 j = 0; j < KeyCount; ++j)
			{
				// Read key
				FString Key;
				Archive << Key;

				FEntryArray& EntryArray = KeyTable.FindOrAdd(*Key);

				FEntry NewEntry;
				NewEntry.LocResID = LocalizationResourceIdentifier;

				// Read string entry.
				Archive << NewEntry.SourceStringHash;

				if (VersionNumber >= ELocResVersion::Compact)
				{
					int32 LocalizedStringIndex = INDEX_NONE;
					Archive << LocalizedStringIndex;

					if (LocalizedStringArray.IsValidIndex(LocalizedStringIndex))
					{
						NewEntry.LocalizedString = LocalizedStringArray[LocalizedStringIndex];
					}
					else
					{
						UE_LOG(LogGlueGenerator, Warning, TEXT("LocRes '%s' has an invalid localized string index for namespace '%s' and key '%s'. This entry will have no translation."), *LocalizationResourceIdentifier, *Namespace, *Key);
					}
				}
				else
				{
					Archive << NewEntry.LocalizedString;
				}

				EntryArray.Add(NewEntry);
			}
		}

		return true;
	}

	static FLocalizationEntryTracker ToolTipLocalization;
	static bool ToolTipLocalizationInitialized = false;

	bool FindToolTip(const FString& Namespace, const FString& Key, FString& OutText)
	{
		return false;
		check(ToolTipLocalizationInitialized);
		FLocalizationEntryTracker::FKeysTable* Table = ToolTipLocalization.Namespaces.Find(Namespace);

		if (nullptr != Table)
		{
			FLocalizationEntryTracker::FEntryArray* Entries = Table->Find(Key);

			if (nullptr != Entries && Entries->Num() > 0)
			{
				OutText = (*Entries)[0].LocalizedString;
				return true;
			}
		}

		return false;
	}
}

void InitializeToolTipLocalization()
{
	if (!LocalizationHack::ToolTipLocalizationInitialized)
	{
		TArray<FString> ToolTipPaths;
		// FPaths::GetToolTipPaths doesn't work in this context unfortunately, because the config file is not the game's config file
		// TODO: perhaps we should load the engine/game config file and use its paths
		ToolTipPaths.Add(TEXT("../../../Engine/Content/Localization/ToolTips"));

		for (int32 PathIndex = 0; PathIndex < ToolTipPaths.Num(); ++PathIndex)
		{
			const FString& LocalizationPath = ToolTipPaths[PathIndex];
			// for code documentation, we always want english
			const FString CulturePath = LocalizationPath / TEXT("en");

			LocalizationHack::ToolTipLocalization.LoadFromDirectory(CulturePath);
		}

		LocalizationHack::ToolTipLocalizationInitialized = true;
	}
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

	if (!LocalizationHack::FindToolTip(Namespace, Key, LocalizedToolTip))
	{
		LocalizedToolTip = NativeToolTip;
	}

	return LocalizedToolTip;
}

FString GetFieldToolTip(const FField& InField)
{
	if (InField.HasMetaData(NAME_ToolTip))
	{
		// mimic behavior of UField::GetToolTipText, which we unfortunately can not use directly because script generator is not actually WITH_EDITOR
		FString LocalizedToolTip;
		const FString NativeToolTip = InField.GetMetaData(NAME_ToolTip);

		static const FString Namespace = TEXT("UObjectToolTips");
		const FString Key = InField.GetFullGroupName(true) + TEXT(".") + InField.GetName();

		if (!LocalizationHack::FindToolTip(Namespace, Key, LocalizedToolTip))
		{
			LocalizedToolTip = NativeToolTip;
		}

		return LocalizedToolTip;
	}
	return FString();
}

FString GetClassToolTip(const UStruct& InStruct)
{
	if (InStruct.HasMetaData(NAME_ToolTip))
	{
		// mimic behavior of UField::GetToolTipText, which we unfortunately can not use directly because script generator is not actually WITH_EDITOR
		FString LocalizedToolTip;
		const FString NativeToolTip = InStruct.GetMetaData(NAME_ToolTip);

		static const FString Namespace = TEXT("UObjectToolTips");
		const FString Key = InStruct.GetFullGroupName(true) + TEXT(".") + InStruct.GetName();

		if (!LocalizationHack::FindToolTip(Namespace, Key, LocalizedToolTip))
		{
			LocalizedToolTip = NativeToolTip;
		}

		return LocalizedToolTip;
	}
	return FString();
}

FProperty* GetFirstParam(UFunction* Function)
{
	for (TFieldIterator<FProperty> It(Function); It && (It->PropertyFlags & CPF_Parm); ++It)
	{
		if (0 == (It->PropertyFlags & CPF_ReturnParm))
		{
			return *It;
		}
	}
	return nullptr;
}

bool GetBoolMetaDataHeirarchical(const UClass* TestClass, FName KeyName, BoolHierarchicalMetaDataMode Mode)
{
	// can't use GetBoolMetaDataHierarchical because its WITH_EDITOR and program plugins don't define that
	bool bResult = false;
	while (TestClass)
	{
		if (TestClass->HasMetaData(KeyName))
		{
			bResult = TestClass->GetBoolMetaData(KeyName);
			if (Mode == BoolHierarchicalMetaDataMode::SearchStopAtAnyValue || bResult)
			{
				break;
			}
		}

		TestClass = TestClass->GetSuperClass();
	}

	return bResult;
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

// helper to extract a project guid from a csproj file
bool ParseGuidFromProjectFile(FGuid& ResultGuid, const FString& ProjectPath)
{
	FString ProjectFileContents;
	if (!FFileHelper::LoadFileToString(ProjectFileContents, *ProjectPath))
	{
		return false;
	}

	const FString StartAnchor(TEXT("<ProjectGuid>")), EndAnchor (TEXT("</ProjectGuid>"));

	const int32 MatchStart = ProjectFileContents.Find(StartAnchor, ESearchCase::CaseSensitive) + StartAnchor.Len();
	if (MatchStart <  StartAnchor.Len())
	{
		return false;
	}

	const int32 MatchEnd = ProjectFileContents.Find(EndAnchor, ESearchCase::CaseSensitive, ESearchDir::FromStart, MatchStart);
	if (MatchEnd <= MatchStart)
	{
		return false;
	}

	const FString GuidString = ProjectFileContents.Mid(MatchStart, MatchEnd - MatchStart);

	return FGuid::ParseExact(GuidString, EGuidFormats::DigitsWithHyphensInBraces, ResultGuid);
}
	
}
