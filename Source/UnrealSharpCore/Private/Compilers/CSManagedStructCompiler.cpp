#include "Compilers/CSManagedStructCompiler.h"
#include "CSManager.h"
#include "ReflectionData/CSStructReflectionData.h"
#include "Types/CSScriptStruct.h"
#include "Factories/CSPropertyFactory.h"
#include "UnrealSharpUtils.h"

#if WITH_EDITOR
#include "Misc/EngineVersionComparison.h"
#include "UserDefinedStructure/UserDefinedStructEditorData.h"

#if !UE_VERSION_OLDER_THAN(5, 8, 0)
PRAGMA_DISABLE_INTERNAL_WARNINGS
#include "StructUtils/StructReinstancer.h"
PRAGMA_ENABLE_INTERNAL_WARNINGS
#endif
#endif

UCSManagedStructCompiler::UCSManagedStructCompiler()
{
	FieldType = UCSScriptStruct::StaticClass();
}

void UCSManagedStructCompiler::Compile(UField* TypeToRecompile, const TSharedPtr<FCSManagedTypeDefinition>& ManagedTypeDefinition) const
{
	UCSScriptStruct* Struct = static_cast<UCSScriptStruct*>(TypeToRecompile);
	TSharedPtr<FCSStructReflectionData> StructReflectionData = ManagedTypeDefinition->GetReflectionData<FCSStructReflectionData>();
	
#if WITH_EDITOR
#if !UE_VERSION_OLDER_THAN(5, 8, 0)
	// Struct reinstancer for this rebuild, as in FUserDefinedStructureCompilerUtils::CompileStruct. PrepareForRecompile
	// saves the instanced structs, instanced struct containers and property bags holding the struct, and
	// FinishRecompile loads them back with the new layout.
	PRAGMA_DISABLE_INTERNAL_WARNINGS
	UE::StructUtils::FStructReinstancerScope ReinstancerScope;
	PRAGMA_ENABLE_INTERNAL_WARNINGS
#endif
	TArray<TSharedPtr<FCSManagedTypeDefinition>> ManagedOwners;
	Struct->PrepareForRecompile(ManagedOwners);
#endif

	PurgeStruct(Struct);
	
	FCSPropertyFactory::CreateAndAssignProperties(Struct, StructReflectionData->Properties);
    
	Struct->Status = UDSS_UpToDate;
	if (!Struct->Guid.IsValid())
	{
		Struct->Guid = FCSUnrealSharpUtils::ConstructGUIDFromString(Struct->GetName());
	}
	
	Struct->Bind();
	Struct->StaticLink(true);
	Struct->Initialize();
	
	RegisterFieldToLoader(TypeToRecompile, ENotifyRegistrationType::NRT_Struct);

#if WITH_EDITOR
	Struct->FinishRecompile();
	UCSManager::Get().OnNewStructEvent().Broadcast(Struct);

	// Structs embedding this one still point at its previous layout and must be rebuilt against the new one,
	// as the editor recompiles user defined structs that contain a changed struct.
	for (const TSharedPtr<FCSManagedTypeDefinition>& ManagedOwner : ManagedOwners)
	{
		ManagedOwner->SetDirtyFlags(StructuralChanges);
		ManagedOwner->Compile();
	}
#endif
}

TSharedPtr<FCSTypeReferenceReflectionData> UCSManagedStructCompiler::CreateReflectionData() const
{
	return MakeShared<FCSStructReflectionData>();
}

void UCSManagedStructCompiler::PurgeStruct(UCSScriptStruct* Field)
{
	FCSUnrealSharpUtils::PurgeStruct(Field);
#if WITH_EDITORONLY_DATA
	Field->ErrorMessage.Empty();
	Field->PrimaryStruct = nullptr;
	Field->EditorData = nullptr;
#endif
}
