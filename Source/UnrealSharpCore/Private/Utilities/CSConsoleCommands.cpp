#if !UE_BUILD_SHIPPING

#include "CSFieldName.h"
#include "CSManagedAssembly.h"
#include "CSManager.h"
#include "Utilities/CSUtilities.h"

FString Indent(int32 Level)
{
	return FString::ChrN(Level * 4, ' ');
}

void MetaDataFromMap(const TMap<FName, FString>* PropertyMetaData, int32 IndentLevel)
{
	if (!PropertyMetaData || PropertyMetaData->Num() == 0)
	{
		UE_LOGFMT(LogUnrealSharp, Log, "{0}(No Metadata Assigned)", Indent(IndentLevel + 1));
		return;
	}
	
	for (const TPair<FName, FString>& MetaDataPair : *PropertyMetaData)
	{
		UE_LOGFMT(LogUnrealSharp, Log, "{0}- {1} = {2}", Indent(IndentLevel + 1), *MetaDataPair.Key.ToString(), *MetaDataPair.Value);
	}
}

void DumpMetaData(UField* Field, int32 IndentLevel)
{
#if WITH_EDITOR
	UE_LOGFMT(LogUnrealSharp, Log, "{0}Metadata:", Indent(IndentLevel));
	
	UPackage* Package = Field->GetOutermost();
	FMetaData& MetaData = Package->GetMetaData();
	
	TMap<FName, FString>* FieldMetaData = MetaData.GetMapForObject(Field);
	MetaDataFromMap(FieldMetaData, IndentLevel);
#endif
}

void DumpMetaData(FProperty* Property, int32 IndentLevel)
{
#if WITH_EDITOR
	UE_LOGFMT(LogUnrealSharp, Log, "{0}Metadata:", Indent(IndentLevel));
	
	const TMap<FName, FString>* PropertyMetaData = Property->GetMetaDataMap();
	MetaDataFromMap(PropertyMetaData, IndentLevel);
#endif
}

void DumpPropertiesOfStruct(UStruct* Struct, int32 IndentLevel)
{
	UE_LOGFMT(LogUnrealSharp, Log, "{0}Properties:", Indent(IndentLevel));
	
	bool bHasProperties = false;
	for (TFieldIterator<FProperty> It(Struct, EFieldIterationFlags::None); It; ++It)
	{
		bHasProperties = true;
		
		FProperty* Property = *It;

		TArray<const TCHAR*> FlagStrings;
		FCSUtilities::ParsePropertyFlags(Property->PropertyFlags, FlagStrings);
		
		UE_LOGFMT(LogUnrealSharp, Log, "{0}- {1} ({2}) ({3})", Indent(IndentLevel + 1), Property->GetName(), Property->GetClass()->GetName(), FString::Join(FlagStrings, TEXT(", ")));
		DumpMetaData(Property, IndentLevel + 2);
	}
	
	if (!bHasProperties)
	{
		UE_LOGFMT(LogUnrealSharp, Log, "{0}(No Properties Assigned)", Indent(IndentLevel + 1));
	}
}

void DumpDataAsStruct(UStruct* Struct, int32 IndentLevel = 1)
{
	DumpPropertiesOfStruct(Struct, IndentLevel + 1);
}

void DumpDataAsTickFunction(const FTickFunction& TickFunction, int32 IndentLevel = 1)
{
	UE_LOGFMT(LogUnrealSharp, Log, "{0}Tick Function:", Indent(IndentLevel + 1));
	UE_LOGFMT(LogUnrealSharp, Log, "{0}- bCanEverTick: {1}", Indent(IndentLevel + 2), TickFunction.bCanEverTick ? TEXT("true") : TEXT("false"));
	UE_LOGFMT(LogUnrealSharp, Log, "{0}- bStartWithTickEnabled: {1}", Indent(IndentLevel + 2), TickFunction.bStartWithTickEnabled ? TEXT("true") : TEXT("false"));
	UE_LOGFMT(LogUnrealSharp, Log, "{0}- TickInterval: {1}", Indent(IndentLevel + 2), TickFunction.TickInterval);
	UE_LOGFMT(LogUnrealSharp, Log, "{0}- TickGroup: {1}", Indent(IndentLevel + 2), static_cast<int32>(TickFunction.TickGroup));
}

void DumpDataAsClass(UClass* Class, int32 IndentLevel = 1)
{
	TArray<const TCHAR*> ClassFlagsStrings;
	FCSUtilities::ParseClassFlags(Class->ClassFlags, ClassFlagsStrings);
	UE_LOGFMT(LogUnrealSharp, Log, "{0}Class Flags: ({1})", Indent(IndentLevel + 1), FString::Join(ClassFlagsStrings, TEXT(", ")));
	
	DumpPropertiesOfStruct(Class, IndentLevel + 1);

	UE_LOGFMT(LogUnrealSharp, Log, "{0}Functions:", Indent(IndentLevel + 1));
	
	bool bHasFunctions = false;
	for (TFieldIterator<UFunction> It(Class, EFieldIterationFlags::None); It; ++It)
	{
		bHasFunctions = true;
		UFunction* Function = *It;

		TArray<const TCHAR*> FunctionFlagsStrings;
		FCSUtilities::ParseFunctionFlags(Function->FunctionFlags, FunctionFlagsStrings);

		UE_LOGFMT(LogUnrealSharp, Log, "{0}- {1} ({2})", Indent(IndentLevel + 2), Function->GetName(), FString::Join(FunctionFlagsStrings, TEXT(", ")));
		DumpMetaData(Function, IndentLevel + 3);
		DumpPropertiesOfStruct(Function, IndentLevel + 3);
	}
	
	AActor* DefaultActor = Cast<AActor>(Class->GetDefaultObject());
	if (IsValid(DefaultActor))
	{
		DumpDataAsTickFunction(DefaultActor->PrimaryActorTick, IndentLevel);
	}
	
	UActorComponent* DefaultComponent = Cast<UActorComponent>(Class->GetDefaultObject());
	if (IsValid(DefaultComponent))
	{
		DumpDataAsTickFunction(DefaultComponent->PrimaryComponentTick, IndentLevel);
	}
	
	if (!bHasFunctions)
	{
		UE_LOGFMT(LogUnrealSharp, Log, "{0}(No Functions Assigned)", Indent(IndentLevel + 2));
	}
}

void DumpDataAsEnum(UEnum* Enum, int32 IndentLevel = 1)
{
	UE_LOGFMT(LogUnrealSharp, Log, "{0}Enum Values:", Indent(IndentLevel + 1));

	for (int32 i = 0; i < Enum->NumEnums(); i++)
	{
		UE_LOGFMT(LogUnrealSharp, Log, "{0}- {1} = {2}", Indent(IndentLevel + 2), Enum->GetNameStringByIndex(i), Enum->GetValueByIndex(i));
	}
}

void DumpDataAsDelegate(UDelegateFunction* Delegate, int32 IndentLevel = 1)
{
	DumpDataAsStruct(Delegate, IndentLevel + 1);
}

void DumpTypeReflectionData(const TArray<FString>& Args)
{
	if (Args.Num() != 1)
	{
		UE_LOG(LogUnrealSharp, Warning, TEXT("Usage: UnrealSharp.DumpTypeReflectionData [TypeFullName]"));
		return;
	}

	FString TypeFullName = Args[0];
	
	if (!TypeFullName.Contains("."))
	{
		UE_LOG(LogUnrealSharp, Warning, TEXT("TypeFullName must be in the format 'Namespace.TypeName'"));
		return;
	}
	
	int32 LastDotIndex;
	TypeFullName.FindLastChar('.', LastDotIndex);
		
	FString Namespace = TypeFullName.Left(LastDotIndex);
	FString TypeName = TypeFullName.Mid(LastDotIndex + 1);
		
	FCSFieldName TypeFieldName(*TypeName, *Namespace);
	
	TArray<UCSManagedAssembly*> Assemblies;
	UCSManager::Get().GetLoadedAssemblies(Assemblies);
	
	for (UCSManagedAssembly* Assembly : Assemblies)
	{
		TSharedPtr<FCSManagedTypeDefinition> TypeDefinition = Assembly->FindManagedTypeDefinition(TypeFieldName);
		
		if (!TypeDefinition.IsValid())
		{
			continue;
		}
		
		UField* Field = TypeDefinition->GetDefinitionField();
		
		if (!IsValid(Field))
		{
			UE_LOGFMT(LogUnrealSharp, Warning, "Managed type found but no associated UField: {0}", *TypeFullName);
			return;
		}
		
		UE_LOGFMT(LogUnrealSharp, Log, "Reflection data for type: {0}", *TypeFullName);
		
		DumpMetaData(Field, 2);
		
		if (UClass* Class = Cast<UClass>(Field))
		{
			DumpDataAsClass(Class);
		}
		else if (UScriptStruct* Struct = Cast<UScriptStruct>(Field))
		{
			DumpDataAsStruct(Struct);
		}
		else if (UEnum* Enum = Cast<UEnum>(Field))
		{
			DumpDataAsEnum(Enum);
		}
		else if (UDelegateFunction* Delegate = Cast<UDelegateFunction>(Field))
		{
			DumpDataAsDelegate(Delegate);
		}
		else
		{
			UE_LOG(LogUnrealSharp, Warning, TEXT("Unsupported type: %s"), *Field->GetClass()->GetName());
		}
		
		return;
	}
	
	UE_LOGFMT(LogUnrealSharp, Warning, "Type not found in any Assembly: {0}", *TypeFullName);
}

static FAutoConsoleCommand CVarDumpTypeReflectionData(
	TEXT("UnrealSharp.DumpTypeReflectionData"),
	TEXT("Shows reflection data for managed type. Example: UnrealSharp.DumpTypeReflectionData MyNamespace.MyClass"),
	FConsoleCommandWithArgsDelegate::CreateStatic(&DumpTypeReflectionData)
);

static FAutoConsoleCommand CVarDumpLoadedAssemblies(
	TEXT("UnrealSharp.DumpLoadedAssemblies"),
	TEXT("Lists all loaded managed assemblies."),
	FConsoleCommandDelegate::CreateStatic([]()
	{
		TArray<UCSManagedAssembly*> Assemblies;
		UCSManager::Get().GetLoadedAssemblies(Assemblies);
		
		UE_LOG(LogUnrealSharp, Log, TEXT("Loaded Managed Assemblies:"));
		for (UCSManagedAssembly* Assembly : Assemblies)
		{
			UE_LOGFMT(LogUnrealSharp, Log, "- {0}", *Assembly->GetAssemblyName().ToString());
		}
	})
);

static FAutoConsoleCommand CVarListTypesInAssembly(
	TEXT("UnrealSharp.ListTypesInAssembly"),
	TEXT("Lists all managed types in the specified assembly. Usage: UnrealSharp.ListTypesInAssembly [AssemblyName]"),
	FConsoleCommandWithArgsDelegate::CreateStatic([](const TArray<FString>& Args)
	{
		if (Args.Num() != 1)
		{
			UE_LOG(LogUnrealSharp, Warning, TEXT("Usage: UnrealSharp.ListTypesInAssembly [AssemblyName]"));
			return;
		}
		
		FName AssemblyName(*Args[0]);
		UCSManagedAssembly* Assembly = UCSManager::Get().FindAssembly(AssemblyName);
		
		if (!IsValid(Assembly))
		{
			UE_LOGFMT(LogUnrealSharp, Warning, "Assembly not found: {0}", *AssemblyName.ToString());
			return;
		}
		
		UE_LOGFMT(LogUnrealSharp, Log, "Managed Types in Assembly {0}:", *AssemblyName.ToString());
		
		for (const TTuple<FCSFieldName, TSharedPtr<FCSManagedTypeDefinition>>& Pair : Assembly->GetDefinedManagedTypes())
		{
			const FCSFieldName& FieldName = Pair.Key;
			TSharedPtr<FCSManagedTypeDefinition> TypeDefinition = Pair.Value;
			
			if (TypeDefinition.IsValid())
			{
				UField* Field = TypeDefinition->GetDefinitionField();
				if (IsValid(Field))
				{
					UE_LOGFMT(LogUnrealSharp, Log, "- {0} ({1})", *FieldName.GetFullName().ToString(), Field->GetClass()->GetName());
				}
				else
				{
					UE_LOGFMT(LogUnrealSharp, Log, "- {0} (No UField)", *FieldName.GetFullName().ToString());
				}
			}
			else
			{
				UE_LOGFMT(LogUnrealSharp, Log, "- {0} (No Type Definition)", *FieldName.GetFullName().ToString());
			}
		}
	})
);

#endif // !UE_BUILD_SHIPPING

