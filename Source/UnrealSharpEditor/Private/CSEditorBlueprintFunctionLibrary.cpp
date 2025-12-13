#include "CSEditorBlueprintFunctionLibrary.h"
#include "CSManager.h"
#include "UnrealSharpEditor.h"

void UCSEditorBlueprintFunctionLibrary::AddAssemblyDependencies(FName AssemblyName, const TArray<FName>& DependentAssemblyNames)
{
	UCSManager& Manager = UCSManager::Get();
	UCSManagedAssembly* Assembly = Manager.FindAssembly(AssemblyName);
	
	if (!IsValid(Assembly))
	{
		UE_LOGFMT(LogUnrealSharpEditor, Warning, "Tried to add dependent assemblies to an invalid assembly: {0}", *AssemblyName.ToString());
		return;
	}
	
	TArray<UCSManagedAssembly*> DependentAssemblies;
	DependentAssemblies.Reserve(DependentAssemblyNames.Num());
	
	for (FName DependentAssemblyName : DependentAssemblyNames)
	{
		UCSManagedAssembly* DependentAssembly = Manager.FindAssembly(DependentAssemblyName);
		
		if (!IsValid(DependentAssembly))
		{
			UE_LOGFMT(LogUnrealSharpEditor, Warning, "Tried to add an invalid dependent assembly: {0}", *DependentAssemblyName.ToString());
			continue;
		}
		
		DependentAssemblies.Add(DependentAssembly);
	}
	
	for (UCSManagedAssembly* DependentAssembly : DependentAssemblies)
	{
		Assembly->AddDependentAssembly(DependentAssembly);
	}
}
