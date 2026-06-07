#include "CSManagedTypeDefinition.h"
#include "CSManager.h"
#include "Compilers/CSManagedTypeCompiler.h"
#include "ReflectionData/CSTypeReferenceReflectionData.h"
#include "Utilities/CSMetaDataUtils.h"

FOnManagedTypeStructureChanged FCSManagedTypeDefinitionEvents::OnReflectionDataChanged;

TSharedPtr<FCSManagedTypeDefinition> FCSManagedTypeDefinition::CreateFromReflectionData(const TSharedPtr<FCSTypeReferenceReflectionData>& InReflectionData, UCSManagedAssembly* InOwningAssembly, UCSManagedTypeCompiler* Compiler)
{
	TSharedPtr<FCSManagedTypeDefinition> NewDefinition = MakeShared<FCSManagedTypeDefinition>();
	NewDefinition->OwningAssembly = InOwningAssembly;
	NewDefinition->Compiler = Compiler;
	NewDefinition->SetReflectionData(InReflectionData);
	
	NewDefinition->DefinitionField = TStrongObjectPtr(Compiler->CreateField(NewDefinition));
	NewDefinition->SetDirtyFlags(StructuralChanges);
	
	return NewDefinition;
}

TSharedPtr<FCSManagedTypeDefinition> FCSManagedTypeDefinition::CreateFromNativeField(UField* InField, UCSManagedAssembly* InOwningAssembly)
{
	TSharedPtr<FCSManagedTypeDefinition> NewDefinition = MakeShared<FCSManagedTypeDefinition>();
	NewDefinition->DefinitionField = TStrongObjectPtr(InField);
	NewDefinition->OwningAssembly = InOwningAssembly;
	NewDefinition->TypeGCHandle = InOwningAssembly->FindTypeHandle(FCSFieldName(InField));
	
	return NewDefinition;
}

UField* FCSManagedTypeDefinition::GetDefinition()
{
	Compile();
	return DefinitionField.Get();
}

void FCSManagedTypeDefinition::Compile()
{
	if (!RequiresCompile() || !GetOwningAssembly()->IsAssemblyLoaded())
	{
		return;
	}
	
	DirtyFlags = None;
	Compiler->StartCompilation(SharedThis(this));
}

#if WITH_EDITOR
TSharedPtr<FGCHandle> FCSManagedTypeDefinition::GetTypeGCHandle()
{
	if (!TypeGCHandle.IsValid() || TypeGCHandle->IsNull())
	{
		FCSFieldName FieldName = ReflectionData.IsValid() ? ReflectionData->FieldName : FCSFieldName(DefinitionField.Get());
		TypeGCHandle = OwningAssembly->FindTypeHandle(FieldName);
	}
	
	return TypeGCHandle;
}
#endif

void FCSManagedTypeDefinition::SetTypeGCHandle(uint8* GCHandlePtr)
{
	TypeGCHandle = OwningAssembly->AddTypeHandle(ReflectionData->FieldName, GCHandlePtr);
}

void FCSManagedTypeDefinition::SetDirtyFlags(ECSTypeStructuralFlags InDirtyFlags)
{
	DirtyFlags = InDirtyFlags;
	
	for (int32 i = ReflectionData->SourceGeneratorDependencies.Num() - 1; i >= 0; --i)
	{
		const FCSFieldName& SourceGeneratorDependency = ReflectionData->SourceGeneratorDependencies[i];
		TSharedPtr<FCSManagedTypeDefinition> ManagedTypeDefinition = OwningAssembly->FindManagedTypeDefinition(SourceGeneratorDependency);
		
		if (!ManagedTypeDefinition.IsValid())
		{
			UE_LOGFMT(LogUnrealSharp, Verbose, "Failed to find dependent type {0} for dirty propagation of {1}", *SourceGeneratorDependency.GetFullName().ToString(), *ReflectionData->FieldName.GetFullName().ToString());
			continue;
		}
		
		if (ManagedTypeDefinition->GetDirtyFlags() >= InDirtyFlags)
		{
			continue;
		}

		ManagedTypeDefinition->SetDirtyFlags(InDirtyFlags);
	}
}
