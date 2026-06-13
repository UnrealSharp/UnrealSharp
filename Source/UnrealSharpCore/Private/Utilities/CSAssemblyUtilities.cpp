#include "Utilities/CSAssemblyUtilities.h"
#include "CSManagedAssembly.h"
#include "CSManager.h"

void DumpLoadedAssemblies(const TArray<FString>& Args)
{
	UE_LOG(LogUnrealSharp, Display, TEXT("Loaded Managed Assemblies:"));
	
	TArray<UCSManagedAssembly*> Assemblies;
	UCSManager::Get().GetLoadedAssemblies(Assemblies);
	
	for (UCSManagedAssembly* Assembly : Assemblies)
	{
		UE_LOGFMT(LogUnrealSharp, Display, "- {0} (Path: {1})", *Assembly->GetName(), *Assembly->GetAssemblyFilePath());
	}
}

static FAutoConsoleCommand CVarDumpLoadedAssemblies(
	TEXT("UnrealSharp.DumpLoadedAssemblies"),
	TEXT("Dumps the names of all currently loaded managed assemblies to the log."),
	FConsoleCommandWithArgsDelegate::CreateStatic(&DumpLoadedAssemblies)
);

#if WITH_EDITOR
void FCSAssemblyUtilities::SortAssembliesByDependencyOrder(const TArray<UCSManagedAssembly*>& InputAssemblies, TArray<UCSManagedAssembly*>& OutSortedAssemblies)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(FCSAssemblyUtilities::SortAssembliesByDependencyOrder)
	
	OutSortedAssemblies.Reset();
	
	TSet<UCSManagedAssembly*> AllAssemblies;
	TArray<UCSManagedAssembly*> WorkStack;

	for (UCSManagedAssembly* Assembly : InputAssemblies)
	{
		if (!IsValid(Assembly) || AllAssemblies.Contains(Assembly))
		{
			continue;
		}
		
		AllAssemblies.Add(Assembly);
		WorkStack.Add(Assembly);
	}

	while (WorkStack.Num() > 0)
	{
		UCSManagedAssembly* CurrentAssembly = WorkStack.Pop();
		const TArray<UCSManagedAssembly*>& DependentAssemblies = CurrentAssembly->GetDependentAssemblies();

		for (UCSManagedAssembly* DependencyAssembly : DependentAssemblies)
		{
			if (!IsValid(DependencyAssembly))
			{
				continue;
			}

			if (AllAssemblies.Contains(DependencyAssembly))
			{
				continue;
			}
			
			AllAssemblies.Add(DependencyAssembly);
			WorkStack.Add(DependencyAssembly);
		}
	}

	if (AllAssemblies.Num() == 0)
	{
		return;
	}
	
	TMap<UCSManagedAssembly*, TArray<UCSManagedAssembly*>> DependencyGraph;
	TMap<UCSManagedAssembly*, int32> IncomingEdgeCount;

	for (UCSManagedAssembly* Assembly : AllAssemblies)
	{
		DependencyGraph.FindOrAdd(Assembly);
		IncomingEdgeCount.FindOrAdd(Assembly) = 0;
	}

	for (UCSManagedAssembly* Assembly : AllAssemblies)
	{
		const TArray<UCSManagedAssembly*>& DependentAssemblies = Assembly->GetDependentAssemblies();
		
		for (UCSManagedAssembly* DependencyAssembly : DependentAssemblies)
		{
			if (!IsValid(DependencyAssembly))
			{
				continue;
			}
			
			DependencyGraph.FindOrAdd(DependencyAssembly);
			IncomingEdgeCount.FindOrAdd(DependencyAssembly);
			
			DependencyGraph[DependencyAssembly].AddUnique(Assembly);
			IncomingEdgeCount[Assembly] += 1;
		}
	}
	
	TArray<UCSManagedAssembly*> AssembliesWithNoUnprocessedDependencies;

	for (const TTuple<UCSManagedAssembly*, int>& Pair : IncomingEdgeCount)
	{
		if (Pair.Value != 0)
		{
			continue;
		}
		
		AssembliesWithNoUnprocessedDependencies.Add(Pair.Key);
	}
	
	while (AssembliesWithNoUnprocessedDependencies.Num() > 0)
	{
		UCSManagedAssembly* AssemblyWithoutDependencies = AssembliesWithNoUnprocessedDependencies.Pop(EAllowShrinking::No);
		OutSortedAssemblies.Add(AssemblyWithoutDependencies);

		const TArray<UCSManagedAssembly*>* DependentAssemblies = DependencyGraph.Find(AssemblyWithoutDependencies);

		if (!DependentAssemblies)
		{
			continue;
		}
		
		for (UCSManagedAssembly* DependentAssembly : *DependentAssemblies)
		{
			int32& Count = IncomingEdgeCount[DependentAssembly];
			Count -= 1;

			if (Count == 0)
			{
				AssembliesWithNoUnprocessedDependencies.Add(DependentAssembly);
			}
		}
	}
	
	if (OutSortedAssemblies.Num() != AllAssemblies.Num())
	{
		UE_LOG(LogUnrealSharp, Warning, TEXT("Cyclic dependency detected among managed assemblies. Remaining assemblies appended in undefined order."));

		for (UCSManagedAssembly* Assembly : AllAssemblies)
		{
			if (OutSortedAssemblies.Contains(Assembly))
			{
				continue;
			}
			
			OutSortedAssemblies.Add(Assembly);
		}
	}
}

bool FCSAssemblyUtilities::IsRuntimeGlueAssembly(const UCSManagedAssembly* Assembly)
{
	const FString& AssemblyName = Assembly->GetName();
	return AssemblyName.EndsWith(".RuntimeGlue", ESearchCase::IgnoreCase);
}
#endif
