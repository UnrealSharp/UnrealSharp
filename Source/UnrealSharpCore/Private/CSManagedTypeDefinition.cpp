#include "CSManagedTypeDefinition.h"
#include "CSManager.h"
#include "Compilers/CSManagedTypeCompiler.h"
#include "ReflectionData/CSTypeReferenceReflectionData.h"
#include "Utilities/CSMetaDataUtils.h"

FOnManagedTypeStructureChanged FCSManagedTypeDefinitionEvents::OnStructureChangedDelegate;

TSharedPtr<FCSManagedTypeDefinition> FCSManagedTypeDefinition::CreateFromReflectionData(const TSharedPtr<FCSTypeReferenceReflectionData>& InReflectionData, UCSManagedAssembly* InOwningAssembly, UCSManagedTypeCompiler* Compiler)
{
	TSharedPtr<FCSManagedTypeDefinition> NewDefinition = MakeShared<FCSManagedTypeDefinition>();
	NewDefinition->OwningAssembly = InOwningAssembly;
	NewDefinition->SetReflectionData(InReflectionData);
	NewDefinition->Compiler = Compiler;
	NewDefinition->ManagedField = TStrongObjectPtr(Compiler->CreateField(NewDefinition));
	NewDefinition->MarkStructurallyDirty();
	
	return NewDefinition;
}

TSharedPtr<FCSManagedTypeDefinition> FCSManagedTypeDefinition::CreateFromNativeField(UField* InField, UCSManagedAssembly* InOwningAssembly)
{
	TSharedPtr<FCSManagedTypeDefinition> NewDefinition = MakeShared<FCSManagedTypeDefinition>();
	NewDefinition->ManagedField = TStrongObjectPtr(InField);
	NewDefinition->OwningAssembly = InOwningAssembly;
	NewDefinition->TypeGCHandle = InOwningAssembly->FindTypeHandle(FCSFieldName(InField));
	NewDefinition->bHasChangedStructure = false;
	
	return NewDefinition;
}

#if WITH_EDITOR
TSharedPtr<FGCHandle> FCSManagedTypeDefinition::GetTypeGCHandle()
{
	if (!TypeGCHandle.IsValid() || TypeGCHandle->IsNull())
	{
		FCSFieldName FieldName = ReflectionData.IsValid() ? ReflectionData->FieldName : FCSFieldName(ManagedField.Get());
		TypeGCHandle = OwningAssembly->FindTypeHandle(FieldName);
	}
	
	return TypeGCHandle;
}
#endif

void FCSManagedTypeDefinition::SetTypeGCHandle(uint8* GCHandlePtr)
{
	TypeGCHandle = OwningAssembly->AddTypeHandle(ReflectionData->FieldName, GCHandlePtr);
}

void FCSManagedTypeDefinition::MarkStructurallyDirty()
{
	if (bHasChangedStructure)
	{
		return;
	}

	// Notify dependent types to rebuild as well. These are spawned by source generators and depend on this type's structure.
	// Such as the async wrapper classes.
	for (int32 i = ReflectionData->SourceGeneratorDependencies.Num() - 1; i >= 0; --i)
	{
		const FCSFieldName& SourceGeneratorDependency = ReflectionData->SourceGeneratorDependencies[i];
		TSharedPtr<FCSManagedTypeDefinition> ManagedTypeDefinition = OwningAssembly->FindManagedTypeDefinition(SourceGeneratorDependency);
		
		if (!ManagedTypeDefinition.IsValid())
		{
			continue;
		}

		ManagedTypeDefinition->MarkStructurallyDirty();
	}
	
	bHasChangedStructure = true;
	FCSManagedTypeDefinitionEvents::OnStructureChangedDelegate.Broadcast(SharedThis(this));
}

UField* FCSManagedTypeDefinition::CompileAndGetManagedField()
{
	if (bHasChangedStructure)
	{
		bHasChangedStructure = false;
		Compiler->RecompileManagedTypeDefinition(SharedThis(this));
	}
	
	return ManagedField.Get();
}
