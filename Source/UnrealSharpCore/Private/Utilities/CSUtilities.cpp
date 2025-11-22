#include "Utilities/CSUtilities.h"

#include "Compilers/CSManagedClassCompiler.h"
#include "Compilers/CSManagedDelegateCompiler.h"
#include "Compilers/CSManagedEnumCompiler.h"
#include "Compilers/CSManagedInterfaceCompiler.h"
#include "Compilers/CSManagedStructCompiler.h"
#include "ReflectionData/CSClassReflectionData.h"
#include "ReflectionData/CSEnumReflectionData.h"

bool FCSUtilities::ResolveCompilerAndReflectionDataForFieldType(ECSFieldType FieldType, UClass*& OutCompilerClass, TSharedPtr<FCSTypeReferenceReflectionData>& OutReflectionData)
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

void FCSUtilities::ParseFunctionFlags(uint32 Flags, TArray<const TCHAR*>& Results)
{
	const TCHAR* FunctionFlags[32] =
	{
		TEXT("Final"),					// FUNC_Final
		TEXT("0x00000002"),
		TEXT("BlueprintAuthorityOnly"),	// FUNC_BlueprintAuthorityOnly
		TEXT("BlueprintCosmetic"),		// FUNC_BlueprintCosmetic
		TEXT("0x00000010"),
		TEXT("0x00000020"),
		TEXT("Net"),					// FUNC_Net
		TEXT("NetReliable"),			// FUNC_NetReliable
		TEXT("NetRequest"),				// FUNC_NetRequest
		TEXT("Exec"),					// FUNC_Exec
		TEXT("Native"),					// FUNC_Native
		TEXT("Event"),					// FUNC_Event
		TEXT("NetResponse"),			// FUNC_NetResponse
		TEXT("Static"),					// FUNC_Static
		TEXT("NetMulticast"),			// FUNC_NetMulticast
		TEXT("0x00008000"),
		TEXT("MulticastDelegate"),		// FUNC_MulticastDelegate
		TEXT("Public"),					// FUNC_Public
		TEXT("Private"),				// FUNC_Private
		TEXT("Protected"),				// FUNC_Protected
		TEXT("Delegate"),				// FUNC_Delegate
		TEXT("NetServer"),				// FUNC_NetServer
		TEXT("HasOutParms"),			// FUNC_HasOutParms
		TEXT("HasDefaults"),			// FUNC_HasDefaults
		TEXT("NetClient"),				// FUNC_NetClient
		TEXT("DLLImport"),				// FUNC_DLLImport
		TEXT("BlueprintCallable"),		// FUNC_BlueprintCallable
		TEXT("BlueprintEvent"),			// FUNC_BlueprintEvent
		TEXT("BlueprintPure"),			// FUNC_BlueprintPure
		TEXT("0x20000000"),
		TEXT("Const"),					// FUNC_Const
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
		TEXT("CPF_Edit"),
		TEXT("CPF_ConstParm"),
		TEXT("CPF_BlueprintVisible"),
		TEXT("CPF_ExportObject"),
		TEXT("CPF_BlueprintReadOnly"),
		TEXT("CPF_Net"),
		TEXT("CPF_EditFixedSize"),
		TEXT("CPF_Parm"),
		TEXT("CPF_OutParm"),
		TEXT("CPF_ZeroConstructor"),
		TEXT("CPF_ReturnParm"),
		TEXT("CPF_DisableEditOnTemplate"),
		TEXT("CPF_NonNullable"),
		TEXT("CPF_Transient"),
		TEXT("CPF_Config"),
		TEXT("CPF_RequiredParm"),
		TEXT("CPF_DisableEditOnInstance"),
		TEXT("CPF_EditConst"),
		TEXT("CPF_GlobalConfig"),
		TEXT("CPF_InstancedReference"),
		TEXT("CPF_ExperimentalExternalObjects"),
		TEXT("CPF_DuplicateTransient"),
		TEXT("0x0000000000400000"),
		TEXT("0x0000000000800000"),
		TEXT("CPF_SaveGame"),	
		TEXT("CPF_NoClear"),
		TEXT("CPF_Virtual"),
		TEXT("CPF_ReferenceParm"),
		TEXT("CPF_BlueprintAssignable"),
		TEXT("CPF_Deprecated"),
		TEXT("CPF_IsPlainOldData"),
		TEXT("CPF_RepSkip"),
		TEXT("CPF_RepNotify"),
		TEXT("CPF_Interp"),
		TEXT("CPF_NonTransactional"),
		TEXT("CPF_EditorOnly"),
		TEXT("CPF_NoDestructor"),
		TEXT("0x0000002000000000"),
		TEXT("CPF_AutoWeak"),
		TEXT("CPF_ContainsInstancedReference"),
		TEXT("CPF_AssetRegistrySearchable"),
		TEXT("CPF_SimpleDisplay"),
		TEXT("CPF_AdvancedDisplay"),
		TEXT("CPF_Protected"),
		TEXT("CPF_BlueprintCallable"),
		TEXT("CPF_BlueprintAuthorityOnly"),
		TEXT("CPF_TextExportTransient"),
		TEXT("CPF_NonPIEDuplicateTransient"),
		TEXT("CPF_ExposeOnSpawn"),
		TEXT("CPF_PersistentInstance"),
		TEXT("CPF_UObjectWrapper"),
		TEXT("CPF_HasGetValueTypeHash"),
		TEXT("CPF_NativeAccessSpecifierPublic"),
		TEXT("CPF_NativeAccessSpecifierProtected"),
		TEXT("CPF_NativeAccessSpecifierPrivate"),
		TEXT("CPF_SkipSerialization"),
		TEXT("CPF_TObjectPtr")
		TEXT("CPF_ExperimentalOverridableLogic"),
		TEXT("CPF_ExperimentalAlwaysOverriden"),
		TEXT("CPF_ExperimentalNeverOverriden"),
		TEXT("CPF_AllowSelfReference"),
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
