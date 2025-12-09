#include "Utilities/CSUtilities.h"

#include "CSManagedAssembly.h"
#include "CSManager.h"
#include "Compilers/CSManagedClassCompiler.h"
#include "Compilers/CSManagedDelegateCompiler.h"
#include "Compilers/CSManagedEnumCompiler.h"
#include "Compilers/CSManagedInterfaceCompiler.h"
#include "Compilers/CSManagedStructCompiler.h"
#include "ReflectionData/CSClassReflectionData.h"
#include "ReflectionData/CSEnumReflectionData.h"

bool FCSUtilities::ResolveCompilerAndReflectionData(ECSFieldType FieldType, UClass*& OutCompilerClass, TSharedPtr<FCSTypeReferenceReflectionData>& OutReflectionData)
{
	switch (FieldType)
	{
	case ECSFieldType::Class:
		OutReflectionData = MakeShared<FCSClassReflectionData>();
		OutCompilerClass = UCSManagedClassCompiler::StaticClass();
		break;
	case ECSFieldType::Struct:
		OutReflectionData = MakeShared<FCSStructReflectionData>();
		OutCompilerClass = UCSManagedStructCompiler::StaticClass();
		break;
	case ECSFieldType::Enum:
		OutReflectionData = MakeShared<FCSEnumReflectionData>();
		OutCompilerClass = UCSManagedEnumCompiler::StaticClass();
		break;
	case ECSFieldType::Interface:
		OutReflectionData = MakeShared<FCSClassBaseReflectionData>();
		OutCompilerClass = UCSManagedInterfaceCompiler::StaticClass();
		break;
	case ECSFieldType::Delegate:
		OutReflectionData = MakeShared<FCSFunctionReflectionData>();
		OutCompilerClass = UCSManagedDelegateCompiler::StaticClass();
		break;
	default:
		UE_LOGFMT(LogUnrealSharp, Error, "Unsupported field type: {0}", static_cast<uint8>(FieldType));
		break;
	}
	
	return OutCompilerClass != nullptr && OutReflectionData.IsValid();
}

bool FCSUtilities::ShouldReloadDefinition(const TSharedRef<FCSManagedTypeDefinition>& ManagedTypeDefinition, const char* RawReflectionData)
{
	if (!ManagedTypeDefinition->HasStructuralChanges())
	{
		return false;
	}
	
	const TSharedPtr<FCSTypeReferenceReflectionData> ReflectionData = ManagedTypeDefinition->GetReflectionData();
	if (ReflectionData->GetRawReflectionData() == RawReflectionData)
	{
		return false;
	}
	
	return true;
}

void FCSUtilities::ParseFunctionFlags(uint32 Flags, TArray<const TCHAR*>& Results)
{
	const TCHAR* FunctionFlags[32] =
	{
		TEXT("Final"),
		TEXT("0x00000002"),
		TEXT("BlueprintAuthorityOnly"),
		TEXT("BlueprintCosmetic"),
		TEXT("0x00000010"),
		TEXT("0x00000020"),
		TEXT("Net"),
		TEXT("NetReliable"),
		TEXT("NetRequest"),
		TEXT("Exec")
		TEXT("Native"),
		TEXT("Event"),
		TEXT("NetResponse"),
		TEXT("Static"),
		TEXT("NetMulticast"),
		TEXT("0x00008000"),
		TEXT("MulticastDelegate"),
		TEXT("Public"),
		TEXT("Private"),
		TEXT("Protected"),
		TEXT("Delegate"),
		TEXT("NetServer"),
		TEXT("HasOutParms"),
		TEXT("HasDefaults"),
		TEXT("NetClient"),
		TEXT("DLLImport"),
		TEXT("BlueprintCallable"),
		TEXT("BlueprintEvent"),
		TEXT("BlueprintPure"),
		TEXT("0x20000000"),
		TEXT("Const"),
		TEXT("0x80000000"),
	};

	for (int32 i = 0; i < 32; ++i)
	{
		const uint32 Mask = 1U << i;
		if ((Flags & Mask) != 0)
		{
			Results.Add(FunctionFlags[i]);
		}
	}
}

void FCSUtilities::ParsePropertyFlags(EPropertyFlags InFlags, TArray<const TCHAR*>& Results)
{
	static const TCHAR* PropertyFlags[] =
	{
		TEXT("Edit"),
		TEXT("ConstParm"),
		TEXT("BlueprintVisible"),
		TEXT("ExportObject"),
		TEXT("BlueprintReadOnly"),
		TEXT("Net"),
		TEXT("EditFixedSize"),
		TEXT("Parm"),
		TEXT("OutParm"),
		TEXT("ZeroConstructor"),
		TEXT("ReturnParm"),
		TEXT("DisableEditOnTemplate"),
		TEXT("NonNullable"),
		TEXT("Transient"),
		TEXT("Config"),
		TEXT("RequiredParm"),
		TEXT("DisableEditOnInstance"),
		TEXT("EditConst"),
		TEXT("GlobalConfig"),
		TEXT("InstancedReference"),
		TEXT("ExperimentalExternalObjects"),
		TEXT("DuplicateTransient"),
		TEXT("0x0000000000400000"),
		TEXT("0x0000000000800000"),
		TEXT("SaveGame"),	
		TEXT("NoClear"),
		TEXT("Virtual"),
		TEXT("ReferenceParm"),
		TEXT("BlueprintAssignable"),
		TEXT("Deprecated"),
		TEXT("IsPlainOldData"),
		TEXT("RepSkip"),
		TEXT("RepNotify"),
		TEXT("Interp"),
		TEXT("NonTransactional"),
		TEXT("EditorOnly"),
		TEXT("NoDestructor"),
		TEXT("0x0000002000000000"),
		TEXT("AutoWeak"),
		TEXT("ContainsInstancedReference"),
		TEXT("AssetRegistrySearchable"),
		TEXT("SimpleDisplay"),
		TEXT("AdvancedDisplay"),
		TEXT("Protected"),
		TEXT("BlueprintCallable"),
		TEXT("BlueprintAuthorityOnly"),
		TEXT("TextExportTransient"),
		TEXT("NonPIEDuplicateTransient"),
		TEXT("ExposeOnSpawn"),
		TEXT("PersistentInstance"),
		TEXT("UObjectWrapper"),
		TEXT("HasGetValueTypeHash"),
		TEXT("NativeAccessSpecifierPublic"),
		TEXT("NativeAccessSpecifierProtected"),
		TEXT("NativeAccessSpecifierPrivate"),
		TEXT("SkipSerialization"),
		TEXT("TObjectPtr")
		TEXT("ExperimentalOverridableLogic"),
		TEXT("ExperimentalAlwaysOverriden"),
		TEXT("ExperimentalNeverOverriden"),
		TEXT("AllowSelfReference"),
	};

	uint64 Flags = InFlags;
	for (const TCHAR* FlagName : PropertyFlags)
	{
		if (Flags & 1)
		{
			Results.Add(FlagName);
		}

		Flags >>= 1;
	}
}

void FCSUtilities::ParseClassFlags(EClassFlags InFlags, TArray<const TCHAR*>& Results)
{
	static const TCHAR* ClassFlags[] =
	{
		TEXT("None"),
		TEXT("Abstract"),
		TEXT("DefaultConfig"),
		TEXT("Config"),
		TEXT("Transient"),
		TEXT("Optional"),
		TEXT("MatchedSerializers"),
		TEXT("ProjectUserConfig"),
		TEXT("Native"),
		TEXT("NotPlaceable"),
		TEXT("PerObjectConfig"),
		TEXT("ReplicationDataIsSetUp"),
		TEXT("EditInlineNew"),
		TEXT("CollapseCategories"),
		TEXT("Interface"),
		TEXT("PerPlatformConfig"),
		TEXT("Const"),
		TEXT("NeedsDeferredDependencyLoading"),
		TEXT("CompiledFromBlueprint"),
		TEXT("MinimalAPI"),
		TEXT("RequiredAPI"),
		TEXT("DefaultToInstanced"),
		TEXT("TokenStreamAssembled"),
		TEXT("HasInstancedReference"),
		TEXT("Hidden"),
		TEXT("Deprecated"),
		TEXT("HideDropDown"),
		TEXT("GlobalUserConfig"),
		TEXT("Intrinsic"),
		TEXT("Constructed"),
		TEXT("ConfigDoNotCheckDefaults"),
		TEXT("NewerVersionExists"),
	};

	uint32 Flags = static_cast<uint32>(InFlags);

	for (int32 Bit = 0; Bit < UE_ARRAY_COUNT(ClassFlags); ++Bit)
	{
		uint32 Mask = (Bit == 0) ? 0 : (1u << (Bit - 1));
		if ((Flags & Mask) != 0)
		{
			Results.Add(ClassFlags[Bit]);
		}
	}
}

