#include "CSManager.h"
#include "CSManagedGCHandle.h"
#include "CSAssembly.h"
#include "UnrealSharpCore.h"
#include "TypeGenerator/CSClass.h"
#include "Misc/Paths.h"
#include "Misc/App.h"
#include "UObject/Object.h"
#include "Misc/MessageDialog.h"
#include "Engine/Blueprint.h"
#include "UnrealSharpProcHelper/CSProcHelper.h"
#include <vector>
#include "CSBindsManager.h"
#include "CSNamespace.h"
#include "CSUnrealSharpSettings.h"
#include "Logging/StructuredLog.h"
#include "StructUtils/UserDefinedStruct.h"
#include "TypeGenerator/CSInterface.h"
#include "TypeGenerator/Factories/CSPropertyFactory.h"
#include "TypeGenerator/Register/CSBuilderManager.h"
#include "TypeGenerator/Register/TypeInfo/CSClassInfo.h"
#include "Utils/CSClassUtilities.h"

#ifdef _WIN32
	#define PLATFORM_STRING(string) string
#elif defined(__unix__)
	#define PLATFORM_STRING(string) TCHAR_TO_ANSI(string)
#elif defined(__APPLE__)
	#define PLATFORM_STRING(string) TCHAR_TO_ANSI(string)
#endif

#ifdef __clang__
#pragma clang diagnostic ignored "-Wdangling-assignment"
#endif

UCSManager* UCSManager::Instance = nullptr;

UPackage* UCSManager::FindOrAddManagedPackage(const FCSNamespace Namespace)
{
	if (UPackage* NativePackage = Namespace.TryGetAsNativePackage())
	{
		return NativePackage;
	}

	FCSNamespace CurrentNamespace = Namespace;
	TArray<FCSNamespace> ParentNamespaces;
	while (true)
	{
		ParentNamespaces.Add(CurrentNamespace);

		if (!CurrentNamespace.GetParentNamespace(CurrentNamespace))
		{
			break;
		}
	}

	UPackage* ParentPackage = nullptr;
	for (int32 i = ParentNamespaces.Num() - 1; i >= 0; i--)
	{
		FCSNamespace ParentNamespace = ParentNamespaces[i];
		FName PackageName = ParentNamespace.GetPackageName();

		for (UPackage* Package : AllPackages)
		{
			if (PackageName == Package->GetFName())
			{
				ParentPackage = Package;
				break;
			}
		}

		if (!ParentPackage)
		{
			ParentPackage = NewObject<UPackage>(nullptr, PackageName, RF_Public);
			ParentPackage->SetPackageFlags(PKG_CompiledIn);
			AllPackages.Add(ParentPackage);
		}
	}

	return ParentPackage;
}

void UCSManager::ForEachManagedField(const TFunction<void(UObject*)>& Callback) const
{
	for (UPackage* Package : AllPackages)
	{
		ForEachObjectWithPackage(Package, [&Callback](UObject* Object)
		{
			Callback(Object);
			return true;
		}, false);
	}
}

UPackage* UCSManager::GetPackage(const FCSNamespace Namespace)
{
	UPackage* FoundPackage;
	if (GetDefault<UCSUnrealSharpSettings>()->HasNamespaceSupport())
	{
		FoundPackage = FindOrAddManagedPackage(Namespace);
	}
	else
	{
		FoundPackage = GetGlobalManagedPackage();
	}

	return FoundPackage;
}

bool UCSManager::IsLoadingAnyAssembly() const
{
	for (const TPair<FName, TObjectPtr<UCSAssembly>>& LoadedAssembly : LoadedAssemblies)
	{
		UCSAssembly* AssemblyPtr = LoadedAssembly.Value;
		if (IsValid(AssemblyPtr) && AssemblyPtr->IsLoading())
		{
			return true;
		}
	}

	return false;
}

void UCSManager::AddDynamicSubsystemClass(TSubclassOf<UDynamicSubsystem> SubsystemClass)
{
	if (!IsValid(SubsystemClass))
	{
		UE_LOG(LogUnrealSharp, Warning, TEXT("Tried to add an invalid dynamic subsystem class"));
		return;
	}

	if (!PendingDynamicSubsystemClasses.Contains(SubsystemClass))
	{
		PendingDynamicSubsystemClasses.Add(SubsystemClass);
	}

	TryInitializeDynamicSubsystems();
}

void UCSManager::Initialize()
{
#if WITH_EDITOR
	FString DotNetInstallationPath = FCSProcHelper::GetDotNetDirectory();
	if (DotNetInstallationPath.IsEmpty())
	{
		FString DialogText = FString::Printf(TEXT("UnrealSharp can't be initialized. An installation of .NET %s SDK can't be found on your system."), TEXT(DOTNET_MAJOR_VERSION));
		FMessageDialog::Open(EAppMsgType::Ok, FText::FromString(DialogText));
		return;
	}

	FString UnrealSharpLibraryPath = FCSProcHelper::GetUnrealSharpPluginsPath();
	if (!FPaths::FileExists(UnrealSharpLibraryPath))
	{
		FString FullPath = FPaths::ConvertRelativePathToFull(UnrealSharpLibraryPath);
		FString DialogText = FString::Printf(TEXT(
			"The bindings library could not be found at the following location:\n%s\n\n"
			"Most likely, the bindings library failed to build due to invalid generated glue."
		), *FullPath);

		FMessageDialog::Open(EAppMsgType::Ok, FText::FromString(DialogText));
		return;
	}

	TArray<FString> ProjectPaths;
	FCSProcHelper::GetAllProjectPaths(ProjectPaths);

	// Compile the C# project for any changes done outside the editor.
	if (!ProjectPaths.IsEmpty() && !FApp::IsUnattended() && !FCSProcHelper::InvokeUnrealSharpBuildTool(BUILD_ACTION_BUILD_WEAVE))
	{
		Initialize();
		return;
	}

	// Remove this listener when the engine is shutting down.
	// Otherwise, we'll get a crash when the GC cleans up all the UObject.
	FCoreDelegates::OnPreExit.AddUObject(this, &UCSManager::OnEnginePreExit);
#endif

	TypeBuilderManager = NewObject<UCSTypeBuilderManager>(this);
	TypeBuilderManager->Initialize();

	GUObjectArray.AddUObjectDeleteListener(this);

	// Initialize the C# runtime.
	if (!InitializeDotNetRuntime())
	{
		return;
	}

	GlobalManagedPackage = FindOrAddManagedPackage(FCSNamespace(TEXT("UnrealSharp")));

	// Initialize the property factory. This is used to create properties for managed structs/classes/functions.
	FCSPropertyFactory::Initialize();

	// Try to load the user assembly. Can be empty if the user hasn't created any csproj yet.
	LoadAllUserAssemblies();

	FModuleManager::Get().OnModulesChanged().AddUObject(this, &UCSManager::OnModulesChanged);
}

bool UCSManager::InitializeDotNetRuntime()
{
	if (!LoadRuntimeHost())
	{
		UE_LOG(LogUnrealSharp, Fatal, TEXT("Failed to load Runtime Host"));
		return false;
	}

	load_assembly_and_get_function_pointer_fn LoadAssemblyAndGetFunctionPointer = InitializeNativeHost();
	if (!LoadAssemblyAndGetFunctionPointer)
	{
		UE_LOG(LogUnrealSharp, Fatal, TEXT("Failed to initialize Runtime Host. Check logs for more details."));
		return false;
	}

	const FString EntryPointClassName = TEXT("UnrealSharp.Plugins.Main, UnrealSharp.Plugins");
	const FString EntryPointFunctionName = TEXT("InitializeUnrealSharp");

	const FString UnrealSharpLibraryAssembly = FPaths::ConvertRelativePathToFull(FCSProcHelper::GetUnrealSharpPluginsPath());
	const FString UserWorkingDirectory = FPaths::ConvertRelativePathToFull(FCSProcHelper::GetUserAssemblyDirectory());

	FInitializeRuntimeHost InitializeUnrealSharp = nullptr;
	const int32 ErrorCode = LoadAssemblyAndGetFunctionPointer(PLATFORM_STRING(*UnrealSharpLibraryAssembly),
		PLATFORM_STRING(*EntryPointClassName),
		PLATFORM_STRING(*EntryPointFunctionName),
		UNMANAGEDCALLERSONLY_METHOD,
		nullptr,
		reinterpret_cast<void**>(&InitializeUnrealSharp));

	if (ErrorCode != 0)
	{
		UE_LOGFMT(LogUnrealSharp, Fatal, "Failed to load assembly: {0}", ErrorCode);
		return false;
	}

	// Entry point to C# to initialize UnrealSharp
	if (!InitializeUnrealSharp(*UserWorkingDirectory,
		*UnrealSharpLibraryAssembly,
		&ManagedPluginsCallbacks,
		(const void*)&FCSBindsManager::GetBoundFunction,
		&FCSManagedCallbacks::ManagedCallbacks))
	{
		UE_LOG(LogUnrealSharp, Fatal, TEXT("Failed to initialize UnrealSharp!"));
		return false;
	}

	return true;
}

bool UCSManager::LoadRuntimeHost()
{
	const FString RuntimeHostPath = FCSProcHelper::GetRuntimeHostPath();
	if (!FPaths::FileExists(RuntimeHostPath))
	{
		UE_LOG(LogUnrealSharp, Error, TEXT("Couldn't find Hostfxr.dll"));
		return false;
	}

	RuntimeHost = FPlatformProcess::GetDllHandle(*RuntimeHostPath);
	if (RuntimeHost == nullptr)
	{
		UE_LOG(LogUnrealSharp, Fatal, TEXT("Failed to get the RuntimeHost DLL handle!"));
		return false;
	}

#if defined(_WIN32)
	void* DLLHandle = FPlatformProcess::GetDllExport(RuntimeHost, TEXT("hostfxr_initialize_for_dotnet_command_line"));
	Hostfxr_Initialize_For_Dotnet_Command_Line = static_cast<hostfxr_initialize_for_dotnet_command_line_fn>(DLLHandle);

	DLLHandle = FPlatformProcess::GetDllExport(RuntimeHost, TEXT("hostfxr_initialize_for_runtime_config"));
	Hostfxr_Initialize_For_Runtime_Config = static_cast<hostfxr_initialize_for_runtime_config_fn>(DLLHandle);

	DLLHandle = FPlatformProcess::GetDllExport(RuntimeHost, TEXT("hostfxr_get_runtime_delegate"));
	Hostfxr_Get_Runtime_Delegate = static_cast<hostfxr_get_runtime_delegate_fn>(DLLHandle);

	DLLHandle = FPlatformProcess::GetDllExport(RuntimeHost, TEXT("hostfxr_close"));
	Hostfxr_Close = static_cast<hostfxr_close_fn>(DLLHandle);
#else
	Hostfxr_Initialize_For_Dotnet_Command_Line = (hostfxr_initialize_for_dotnet_command_line_fn)FPlatformProcess::GetDllExport(RuntimeHost, TEXT("hostfxr_initialize_for_dotnet_command_line"));

	Hostfxr_Initialize_For_Runtime_Config = (hostfxr_initialize_for_runtime_config_fn)FPlatformProcess::GetDllExport(RuntimeHost, TEXT("hostfxr_initialize_for_runtime_config"));

	Hostfxr_Get_Runtime_Delegate = (hostfxr_get_runtime_delegate_fn)FPlatformProcess::GetDllExport(RuntimeHost, TEXT("hostfxr_get_runtime_delegate"));

	Hostfxr_Close = (hostfxr_close_fn)FPlatformProcess::GetDllExport(RuntimeHost, TEXT("hostfxr_close"));
#endif

	return Hostfxr_Initialize_For_Dotnet_Command_Line && Hostfxr_Get_Runtime_Delegate && Hostfxr_Close && Hostfxr_Initialize_For_Runtime_Config;
}

bool UCSManager::LoadAllUserAssemblies()
{
	TArray<FString> UserAssemblyPaths;
	FCSProcHelper::GetAssemblyPathsByLoadOrder(UserAssemblyPaths, true);

	if (UserAssemblyPaths.IsEmpty())
	{
		return true;
	}
	
	for (const FString& UserAssemblyPath : UserAssemblyPaths)
	{
		LoadAssemblyByPath(UserAssemblyPath);
	}

	OnAssembliesLoaded.Broadcast();
	return true;
}

void UCSManager::NotifyUObjectDeleted(const UObjectBase* Object, int32 Index)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSManager::NotifyUObjectDeleted);

	TSharedPtr<FGCHandle> Handle;
	if (!ManagedObjectHandles.RemoveAndCopyValueByHash(Index, Index, Handle))
	{
		return;
	}

	UCSAssembly* Assembly = FindOwningAssembly(Object->GetClass());
	if (!IsValid(Assembly))
	{
		FString ObjectName = Object->GetFName().ToString();
		FString ClassName = Object->GetClass()->GetFName().ToString();
		UE_LOG(LogUnrealSharp, Error, TEXT("Failed to find owning assembly for object %s with class %s. Will cause managed memory leak."), *ObjectName, *ClassName);
		return;
	}

	TSharedPtr<const FGCHandle> AssemblyHandle = Assembly->GetManagedAssemblyHandle();
	Handle->Dispose(AssemblyHandle->GetHandle());

    TMap<uint32, TSharedPtr<FGCHandle>>* FoundHandles = ManagedInterfaceWrappers.FindByHash(Index, Index);
	if (FoundHandles == nullptr)
	{
		return;
	}

	for (auto &[Key, Value] : *FoundHandles)
	{
		Value->Dispose(AssemblyHandle->GetHandle());
	}
	
	FoundHandles->Empty();
	ManagedInterfaceWrappers.Remove(Index);
}

void UCSManager::OnModulesChanged(FName InModuleName, EModuleChangeReason InModuleChangeReason)
{
	if (InModuleChangeReason != EModuleChangeReason::ModuleLoaded)
	{
		return;
	}

	TryInitializeDynamicSubsystems();
}

void UCSManager::TryInitializeDynamicSubsystems()
{
	// Try to activate Editor/EngineSubsystems
	for (int32 i = PendingDynamicSubsystemClasses.Num() - 1; i >= 0; --i)
	{
		TSubclassOf<UDynamicSubsystem> SubsystemClass = PendingDynamicSubsystemClasses[i];
		if (!IsValid(SubsystemClass))
		{
			UE_LOG(LogUnrealSharp, Warning, TEXT("Tried to activate an invalid dynamic subsystem class"));
			PendingDynamicSubsystemClasses.RemoveAt(i);
			continue;
		}

		FSubsystemCollectionBase::ActivateExternalSubsystem(SubsystemClass);

		// Unfortunately no better way to check if the subsystems actually registered.
		{
			if (SubsystemClass->IsChildOf(UEngineSubsystem::StaticClass()))
			{
				if (IsValid(GEngine) && GEngine->GetEngineSubsystemBase(SubsystemClass.Get()))
				{
					PendingDynamicSubsystemClasses.RemoveAt(i);
				}
			}
#if WITH_EDITOR
			else if (SubsystemClass->IsChildOf(UEditorSubsystem::StaticClass()))
			{
				if (IsValid(GEditor) && GEditor->GetEditorSubsystemBase(SubsystemClass.Get()))
				{
					PendingDynamicSubsystemClasses.RemoveAt(i);
				}
			}
#endif
		}
	}
}

load_assembly_and_get_function_pointer_fn UCSManager::InitializeNativeHost() const
{
#if WITH_EDITOR
	FString DotNetPath = FCSProcHelper::GetDotNetDirectory();
#else
	FString DotNetPath = FCSProcHelper::GetPluginAssembliesPath();
#endif

	if (!FPaths::DirectoryExists(DotNetPath))
	{
		UE_LOG(LogUnrealSharp, Error, TEXT("Dotnet directory does not exist at: %s"), *DotNetPath);
		return nullptr;
	}

	FString RuntimeHostPath =  FCSProcHelper::GetRuntimeHostPath();
	if (!FPaths::FileExists(RuntimeHostPath))
	{
		UE_LOG(LogUnrealSharp, Error, TEXT("Runtime host path does not exist at: %s"), *RuntimeHostPath);
		return nullptr;
	}

	UE_LOG(LogUnrealSharp, Log, TEXT("DotNetPath: %s"), *DotNetPath);
	UE_LOG(LogUnrealSharp, Log, TEXT("RuntimeHostPath: %s"), *RuntimeHostPath);

	hostfxr_initialize_parameters InitializeParameters;
	InitializeParameters.dotnet_root = PLATFORM_STRING(*DotNetPath);
	InitializeParameters.host_path = PLATFORM_STRING(*RuntimeHostPath);
	InitializeParameters.size = sizeof(hostfxr_initialize_parameters);

	hostfxr_handle HostFXR_Handle = nullptr;
	int32 ErrorCode = 0;
#if WITH_EDITOR
	FString RuntimeConfigPath = FCSProcHelper::GetRuntimeConfigPath();

	if (!FPaths::FileExists(RuntimeConfigPath))
	{
		UE_LOG(LogUnrealSharp, Error, TEXT("No runtime config found"));
		return nullptr;
	}
#if defined(_WIN32)
	ErrorCode = Hostfxr_Initialize_For_Runtime_Config(PLATFORM_STRING(*RuntimeConfigPath), &InitializeParameters, &HostFXR_Handle);
#else
	ErrorCode = Hostfxr_Initialize_For_Runtime_Config(PLATFORM_STRING(*RuntimeConfigPath), nullptr, &HostFXR_Handle);
#endif

#else
	FString PluginAssemblyPath = FCSProcHelper::GetUnrealSharpPluginsPath();

	if (!FPaths::FileExists(PluginAssemblyPath))
	{
		UE_LOG(LogUnrealSharp, Error, TEXT("UnrealSharp.Plugins.dll does not exist at: %s"), *PluginAssemblyPath);
		return nullptr;
	}

	std::vector Args { PLATFORM_STRING(*PluginAssemblyPath) };

#if defined(_WIN32)
	ErrorCode = Hostfxr_Initialize_For_Dotnet_Command_Line(Args.size(), Args.data(), &InitializeParameters, &HostFXR_Handle);
#else
	ErrorCode = Hostfxr_Initialize_For_Dotnet_Command_Line(Args.size(), const_cast<const char**>(Args.data()), &InitializeParameters, &HostFXR_Handle);
#endif

#endif

	if (ErrorCode != 0)
	{
		UE_LOG(LogUnrealSharp, Error, TEXT("hostfxr_initialize_for_runtime_config failed with code: %d"), ErrorCode);
		return nullptr;
	}

	void* LoadAssemblyAndGetFunctionPointer = nullptr;
	ErrorCode = Hostfxr_Get_Runtime_Delegate(HostFXR_Handle, hdt_load_assembly_and_get_function_pointer, &LoadAssemblyAndGetFunctionPointer);
	Hostfxr_Close(HostFXR_Handle);

	if (ErrorCode != 0 || !LoadAssemblyAndGetFunctionPointer)
	{
		UE_LOG(LogUnrealSharp, Error, TEXT("hostfxr_get_runtime_delegate failed with code: %d"), ErrorCode);
		return nullptr;
	}

	return (load_assembly_and_get_function_pointer_fn)LoadAssemblyAndGetFunctionPointer;
}

UCSAssembly* UCSManager::LoadAssemblyByPath(const FString& AssemblyPath, bool bIsCollectible)
{
	if (!FPaths::FileExists(AssemblyPath))
	{
		UE_LOG(LogUnrealSharp, Error, TEXT("Assembly path does not exist: %s"), *AssemblyPath);
		return nullptr;
	}

	FString AssemblyName = FPaths::GetBaseFilename(AssemblyPath);
	UCSAssembly* ExistingAssembly = FindAssembly(FName(*AssemblyName));
	
	if (IsValid(ExistingAssembly) && ExistingAssembly->IsValidAssembly())
	{
		UE_LOGFMT(LogUnrealSharp, Display, "Assembly {AssemblyName} is already loaded.", *AssemblyName);
		return ExistingAssembly;
	}
	
	UCSAssembly* NewAssembly = NewObject<UCSAssembly>(this, *AssemblyName);
	NewAssembly->SetAssemblyPath(AssemblyPath);
	
	LoadedAssemblies.Add(NewAssembly->GetAssemblyName(), NewAssembly);

	if (!NewAssembly->LoadAssembly(bIsCollectible))
	{
		return nullptr;
	}

	OnManagedAssemblyLoaded.Broadcast(NewAssembly->GetAssemblyName());

	UE_LOGFMT(LogUnrealSharp, Display, "Successfully loaded AssemblyHandle with path {AssemblyPath}.", *AssemblyPath);
	return NewAssembly;
}

UCSAssembly* UCSManager::LoadUserAssemblyByName(const FName AssemblyName, bool bIsCollectible)
{
	FString AssemblyPath = FPaths::Combine(FCSProcHelper::GetUserAssemblyDirectory(), AssemblyName.ToString() + ".dll");
	return LoadAssemblyByPath(AssemblyPath, bIsCollectible);
}

UCSAssembly* UCSManager::LoadPluginAssemblyByName(const FName AssemblyName, bool bIsCollectible)
{
	FString AssemblyPath = FPaths::Combine(FCSProcHelper::GetPluginAssembliesPath(), AssemblyName.ToString() + ".dll");
	return LoadAssemblyByPath(AssemblyPath, bIsCollectible);
}

UCSAssembly* UCSManager::FindOwningAssembly(UClass* Class)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSManager::FindOwningAssembly);
	
	if (ICSManagedTypeInterface* ManagedType = FCSClassUtilities::GetManagedType(Class))
	{
		// Fast access to the owning assembly for managed types.
		return ManagedType->GetOwningAssembly();
	}

	Class = FCSClassUtilities::GetFirstNativeClass(Class);
	uint32 ClassID = Class->GetUniqueID();
	TObjectPtr<UCSAssembly> Assembly = NativeClassToAssemblyMap.FindOrAddByHash(ClassID, ClassID);

	if (IsValid(Assembly))
	{
		return Assembly;
	}
    
    return FindOwningAssemblySlow(Class);
}

UCSAssembly * UCSManager::FindOwningAssembly(UScriptStruct* Struct)
{
    TRACE_CPUPROFILER_EVENT_SCOPE(UCSManager::FindOwningAssembly);
	
    if (const ICSManagedTypeInterface* ManagedType = Cast<ICSManagedTypeInterface>(Struct); ManagedType != nullptr)
    {
        // Fast access to the owning assembly for managed types.
        return ManagedType->GetOwningAssembly();
    }

    if (const UUserDefinedStruct* UserStruct = Cast<UUserDefinedStruct>(Struct); UserStruct != nullptr)
    {
        // This is a Blueprint Struct and we can't use it
        return nullptr;
    }
    
    uint32 ClassID = Struct->GetUniqueID();
    TObjectPtr<UCSAssembly> Assembly = NativeClassToAssemblyMap.FindOrAddByHash(ClassID, ClassID);

    if (IsValid(Assembly))
    {
        return Assembly;
    }

    return FindOwningAssemblySlow(Struct);
}



UCSAssembly * UCSManager::FindOwningAssemblySlow(UField *Field)
{
    // Slow path for native classes. This runs once per new native class.
    const FCSFieldName ClassName = FCSFieldName(Field);

    for (TPair<FName, TObjectPtr<UCSAssembly>>& LoadedAssembly : LoadedAssemblies)
    {
        if (TSharedPtr<FGCHandle> TypeHandle = LoadedAssembly.Value->TryFindTypeHandle(ClassName); !TypeHandle.IsValid() || TypeHandle->IsNull())
        {
            continue;
        }

        return LoadedAssembly.Value;
    }

    return nullptr;
}

FGCHandle UCSManager::FindManagedObject(const UObject* Object)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSManager::FindManagedObject);

	if (!IsValid(Object))
	{
		return FGCHandle::Null();
	}

	uint32 ObjectID = Object->GetUniqueID();
	if (TSharedPtr<FGCHandle>* FoundHandle = ManagedObjectHandles.FindByHash(ObjectID, ObjectID))
	{
#if WITH_EDITOR
		// During full hot reload only the managed objects are GCd as we reload the assemblies.
		// So the C# counterpart can be invalid even if the handle can be found, so we need to create a new one.
		TSharedPtr<FGCHandle> HandlePtr = *FoundHandle;
		if (HandlePtr.IsValid() && !HandlePtr->IsNull())
		{
			return *HandlePtr;
		}
#else
		return **FoundHandle;
#endif
	}

	// No existing handle found, we need to create a new managed object.
	UCSAssembly* OwningAssembly = FindOwningAssembly(Object->GetClass());
	if (!IsValid(OwningAssembly))
	{
		UE_LOGFMT(LogUnrealSharp, Error, "Failed to find assembly for {0}", *Object->GetName());
		return FGCHandle::Null();
	}

	return *OwningAssembly->CreateManagedObject(Object);
}

FGCHandle UCSManager::FindOrCreateManagedInterfaceWrapper(UObject* Object, UClass* InterfaceClass)
{
	if (!Object->GetClass()->ImplementsInterface(InterfaceClass))
	{
		return FGCHandle::Null();
	}

	// No existing handle found, we need to create a new managed object.
	UCSAssembly* OwningAssembly = FindOwningAssembly(InterfaceClass);
	if (!IsValid(OwningAssembly))
	{
		UE_LOGFMT(LogUnrealSharp, Error, "Failed to find assembly for {0}", *InterfaceClass->GetName());
		return FGCHandle::Null();
	}
	
	TSharedPtr<FGCHandle> FoundHandle = OwningAssembly->FindOrCreateManagedInterfaceWrapper(Object, InterfaceClass);
	if (!FoundHandle.IsValid())
	{
		return FGCHandle::Null();
	}

	return *FoundHandle;
}
