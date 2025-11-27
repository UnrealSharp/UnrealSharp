#include "CSEditorBlueprintFunctionLibrary.h"
#include "CSManager.h"
#include "UnrealSharpEditor.h"

void UCSEditorBlueprintFunctionLibrary::SetupAssemblyReferences(FName AssemblyName, const TArray<FName>& DependentAssemblyNames, const TArray<FName>& ReferencedAssemblyNames)
{
	UCSManager& Manager = UCSManager::Get();
	UCSManagedAssembly* Assembly = Manager.FindAssembly(AssemblyName);
	
	if (!IsValid(Assembly))
	{
		UE_LOGFMT(LogUnrealSharpEditor, Warning, "Tried to add dependent assemblies to an invalid assembly: {0}", *AssemblyName.ToString());
		return;
	}
	
	auto GatherAssembliesFromNames = [&Manager](const TArray<FName>& AssemblyNames, TArray<UCSManagedAssembly*>& OutAssemblies)
	{
		for (const FName& Name : AssemblyNames)
		{
			UCSManagedAssembly* FoundAssembly = Manager.FindAssembly(Name);
			
			if (!IsValid(FoundAssembly))
			{
				UE_LOGFMT(LogUnrealSharpEditor, Warning, "Tried to add an invalid referenced assembly: {0}", *Name.ToString());
				continue;
			}
			
			OutAssemblies.Add(FoundAssembly);
		}
	};
	
	TArray<UCSManagedAssembly*> DependentAssemblies;
	GatherAssembliesFromNames(DependentAssemblyNames, DependentAssemblies);
	
	TArray<UCSManagedAssembly*> ReferencedAssemblies;
	GatherAssembliesFromNames(ReferencedAssemblyNames, ReferencedAssemblies);
	
	for (UCSManagedAssembly* DependentAssembly : DependentAssemblies)
	{
		Assembly->AddDependentAssembly(DependentAssembly);
	}
	
	for (UCSManagedAssembly* ReferencedAssembly : ReferencedAssemblies)
	{
		Assembly->AddReferencedAssembly(ReferencedAssembly);
	}
}
