#include "CSManager.h"
#include "CSManagedGCHandle.h"
#include "CSManagedAssembly.h"
#include "UnrealSharpCore.h"
#include "Misc/App.h"
#include "UObject/Object.h"
#include "Misc/MessageDialog.h"
#include "Engine/Blueprint.h"
#include <vector>
#include "CSBindsManager.h"
#include "CSDotnetUtilties.h"
#include "CSNamespace.h"
#include "CSPathsUtilities.h"
#include "CSProjectUtilities.h"
#include "CSUnrealSharpSettings.h"
#include "Engine/UserDefinedEnum.h"
#include "Logging/StructuredLog.h"

#if ENGINE_MAJOR_VERSION >= 5 && ENGINE_MINOR_VERSION >= 5
#include "StructUtils/UserDefinedStruct.h"
#else
#include "Engine/UserDefinedStruct.h"
#endif

#include "Factories/CSPropertyFactory.h"
#include "Utilities/CSClassUtilities.h"

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

UPackage* UCSManager::FindOrAddManagedPackage(const FCSNamespace& Namespace)
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
		const FCSNamespace& ParentNamespace = ParentNamespaces[i];
		const FName PackageName = ParentNamespace.GetPackageName();

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
	if (GetDefault<UCSUnrealSharpSettings>()->HasNamespaceSupport())
	{
		return FindOrAddManagedPackage(Namespace);
	}
	
	return GetGlobalManagedPackage();
}

bool UCSManager::IsLoadingAnyAssembly() const
{
	bool bIsLoading = false;
	for (const TTuple<FName, TObjectPtr<UCSManagedAssembly>>& NameToAssembly : LoadedAssemblies)
	{
		UCSManagedAssembly* Assembly = NameToAssembly.Value;
		
		if (!Assembly->IsAssemblyLoading() || Assembly->IsAssemblyLoaded())
		{
			continue;
		}
		
		bIsLoading = true;
		break;
	}
	
	return bIsLoading;
}

void UCSManager::ActivateSubsystemClass(TSubclassOf<USubsystem> SubsystemClass)
{
	PendingSubsystems.AddUnique(SubsystemClass);
	InitializeSubsystems();
}

void UCSManager::Initialize()
{
#if WITH_EDITOR
	if (!UnrealSharp::DotNetUtilities::VerifyCSharpEnvironment())
	{
		Initialize();
		return;
	}
#endif
	
	if (!InitializeDotNetRuntime())
	{
		return;
	}

	FCoreDelegates::OnPreExit.AddUObject(this, &UCSManager::OnEnginePreExit);
	GUObjectArray.AddUObjectDeleteListener(this);
	FModuleManager::Get().OnModulesChanged().AddUObject(this, &UCSManager::OnModulesChanged);
	FCSPropertyFactory::Initialize();

	GlobalManagedPackage = FindOrAddManagedPackage(FCSNamespace(TEXT("UnrealSharp")));
	LoadAllUserAssemblies();
	
	bIsInitialized = true;
	OnCSManagerInitialized.Broadcast(*this);
}

bool UCSManager::InitializeDotNetRuntime()
{
	if (!LoadRuntimeHost())
	{
		UE_LOGFMT(LogUnrealSharp, Fatal, "Failed to load Runtime Host");
	}

	load_assembly_and_get_function_pointer_fn LoadAssemblyAndGetFunctionPointer = InitializeNativeHost();
	if (!LoadAssemblyAndGetFunctionPointer)
	{
		UE_LOGFMT(LogUnrealSharp, Fatal, "Failed to initialize Runtime Host. Check logs for more details.");
	}

	const FString EntryPointClassName = TEXT("UnrealSharp.Plugins.Main, UnrealSharp.Plugins");
	const FString EntryPointFunctionName = TEXT("InitializeUnrealSharp");
	const FString UnrealSharpLibraryAssembly = FPaths::ConvertRelativePathToFull(UnrealSharp::Paths::GetUnrealSharpPluginsPath());
	const FString UserWorkingDirectory = FPaths::ConvertRelativePathToFull(UnrealSharp::Paths::GetUserAssemblyDirectory());

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

	if (!InitializeUnrealSharp(*UserWorkingDirectory,
		*UnrealSharpLibraryAssembly,
		&ManagedPluginsCallbacks,
		(const void*)&FCSBindsManager::GetBoundFunction,
		&FCSManagedCallbacks::ManagedCallbacks))
	{
		UE_LOGFMT(LogUnrealSharp, Fatal, "Failed to initialize UnrealSharp!");
		return false;
	}

	return true;
}

bool UCSManager::LoadRuntimeHost()
{
	const FString RuntimeHostPath = UnrealSharp::Paths::GetRuntimeHostPath();
	if (!FPaths::FileExists(RuntimeHostPath))
	{
		UE_LOGFMT(LogUnrealSharp, Fatal, "Couldn't find Hostfxr at: {0}", RuntimeHostPath);
	}

	RuntimeHost = FPlatformProcess::GetDllHandle(*RuntimeHostPath);
	if (!RuntimeHost)
	{
		UE_LOGFMT(LogUnrealSharp, Fatal, "Failed to get the RuntimeHost DLL handle!");
	}

	auto GetExport = [this](const TCHAR* ExportName) -> void*
	{
		return FPlatformProcess::GetDllExport(RuntimeHost, ExportName);
	};

	Hostfxr_Initialize_For_Dotnet_Command_Line = reinterpret_cast<hostfxr_initialize_for_dotnet_command_line_fn>(GetExport(TEXT("hostfxr_initialize_for_dotnet_command_line")));
	Hostfxr_Initialize_For_Runtime_Config = reinterpret_cast<hostfxr_initialize_for_runtime_config_fn>(GetExport(TEXT("hostfxr_initialize_for_runtime_config")));
	Hostfxr_Get_Runtime_Delegate = reinterpret_cast<hostfxr_get_runtime_delegate_fn>(GetExport(TEXT("hostfxr_get_runtime_delegate")));
	Hostfxr_Close = reinterpret_cast<hostfxr_close_fn>(GetExport(TEXT("hostfxr_close")));
	return Hostfxr_Initialize_For_Dotnet_Command_Line && Hostfxr_Initialize_For_Runtime_Config && Hostfxr_Get_Runtime_Delegate && Hostfxr_Close;
}

bool UCSManager::LoadAllUserAssemblies()
{
	TArray<FString> UserAssemblyPaths;
	UnrealSharp::Project::GetAssemblyPathsByLoadOrder(UserAssemblyPaths, true);

	if (UserAssemblyPaths.IsEmpty())
	{
		return true;
	}
	
	for (const FString& UserAssemblyPath : UserAssemblyPaths)
	{
		const bool bIsCollectible = !FPaths::GetBaseFilename(UserAssemblyPath).EndsWith(TEXT(".Glue"));
		LoadAssemblyByPath(UserAssemblyPath, bIsCollectible);
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

	UCSManagedAssembly* Assembly = FindOwningAssembly(Object->GetClass());
	if (!IsValid(Assembly))
	{
		UE_LOGFMT(LogUnrealSharp, Error, "Failed to find owning assembly for object {0} with class {1}. Will cause managed memory leak.", Object->GetFName(), Object->GetClass()->GetFName());
		return;
	}

	TSharedPtr<const FGCHandle> AssemblyHandle = Assembly->GetManagedAssemblyHandle();
	
#if WITH_EDITOR
	if (!AssemblyHandle.IsValid())
	{
		return;
	}
#endif
	
	Handle->Dispose(AssemblyHandle->GetHandle());

	TMap<uint32, TSharedPtr<FGCHandle>>* FoundHandles = ManagedInterfaceWrappers.FindByHash(Index, Index);
	if (!FoundHandles)
	{
		return;
	}

	for (const auto& [Key, Value] : *FoundHandles)
	{
		Value->Dispose(AssemblyHandle->GetHandle());
	}
	
	ManagedInterfaceWrappers.RemoveByHash(Index, Index);
}

void UCSManager::OnModulesChanged(FName InModuleName, EModuleChangeReason InModuleChangeReason)
{
	if (InModuleChangeReason == EModuleChangeReason::ModuleLoaded)
	{
		InitializeSubsystems();
	}
}

void UCSManager::InitializeSubsystems()
{
	for (int32 i = PendingSubsystems.Num() - 1; i >= 0; --i)
	{
		TSubclassOf<USubsystem> SubsystemClass = PendingSubsystems[i];

		FSubsystemCollectionBase::ActivateExternalSubsystem(SubsystemClass);
		
		TArray<UObject*> FoundSubsystems;
		GetObjectsOfClass(SubsystemClass, FoundSubsystems);
		
		if (!FoundSubsystems.IsEmpty())
		{
			PendingSubsystems.RemoveAt(i);
		}
	}
}

load_assembly_and_get_function_pointer_fn UCSManager::InitializeNativeHost() const
{
#if WITH_EDITOR
	const FString DotNetPath = UnrealSharp::Paths::GetDotNetDirectory();
#else
	const FString DotNetPath = UnrealSharp::Paths::GetPluginAssembliesPath();
#endif

	if (!FPaths::DirectoryExists(DotNetPath))
	{
		UE_LOGFMT(LogUnrealSharp, Error, "Dotnet directory does not exist at: {0}", DotNetPath);
		return nullptr;
	}

	const FString RuntimeHostPath = UnrealSharp::Paths::GetRuntimeHostPath();
	if (!FPaths::FileExists(RuntimeHostPath))
	{
		UE_LOGFMT(LogUnrealSharp, Error, "Runtime host path does not exist at: {0}", RuntimeHostPath);
		return nullptr;
	}

	UE_LOGFMT(LogUnrealSharp, Log, "DotNetPath: {0}", DotNetPath);
	UE_LOGFMT(LogUnrealSharp, Log, "RuntimeHostPath: {0}", RuntimeHostPath);

	hostfxr_initialize_parameters InitializeParameters;
	InitializeParameters.dotnet_root = PLATFORM_STRING(*DotNetPath);
	InitializeParameters.host_path = PLATFORM_STRING(*RuntimeHostPath);
	InitializeParameters.size = sizeof(hostfxr_initialize_parameters);

	hostfxr_handle HostFXR_Handle = nullptr;
	int32 ErrorCode = 0;

#if WITH_EDITOR
	const FString RuntimeConfigPath = UnrealSharp::Paths::GetRuntimeConfigPath();
	if (!FPaths::FileExists(RuntimeConfigPath))
	{
		UE_LOGFMT(LogUnrealSharp, Error, "No runtime config found");
		return nullptr;
	}

#if defined(_WIN32)
	ErrorCode = Hostfxr_Initialize_For_Runtime_Config(PLATFORM_STRING(*RuntimeConfigPath), &InitializeParameters, &HostFXR_Handle);
#else
	ErrorCode = Hostfxr_Initialize_For_Runtime_Config(PLATFORM_STRING(*RuntimeConfigPath), nullptr, &HostFXR_Handle);
#endif

#else
	const FString PluginAssemblyPath = UnrealSharp::Paths::GetUnrealSharpPluginsPath();
	if (!FPaths::FileExists(PluginAssemblyPath))
	{
		UE_LOGFMT(LogUnrealSharp, Error, "UnrealSharp.Plugins.dll does not exist at: {0}", PluginAssemblyPath);
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
		UE_LOGFMT(LogUnrealSharp, Error, "hostfxr_initialize failed with code: {0}", ErrorCode);
		return nullptr;
	}

	void* LoadAssemblyAndGetFunctionPointer = nullptr;
	ErrorCode = Hostfxr_Get_Runtime_Delegate(HostFXR_Handle, hdt_load_assembly_and_get_function_pointer, &LoadAssemblyAndGetFunctionPointer);
	Hostfxr_Close(HostFXR_Handle);

	if (ErrorCode != 0 || !LoadAssemblyAndGetFunctionPointer)
	{
		UE_LOGFMT(LogUnrealSharp, Error, "hostfxr_get_runtime_delegate failed with code: {0}", ErrorCode);
		return nullptr;
	}

	return reinterpret_cast<load_assembly_and_get_function_pointer_fn>(LoadAssemblyAndGetFunctionPointer);
}

UCSManagedAssembly* UCSManager::LoadAssemblyByPath(const FString& AssemblyPath, bool bIsCollectible)
{
	if (!FPaths::FileExists(AssemblyPath))
	{
		UE_LOGFMT(LogUnrealSharp, Error, "Assembly path does not exist: {0}", AssemblyPath);
		return nullptr;
	}

	const FString AssemblyName = FPaths::GetBaseFilename(AssemblyPath);
	
	if (UCSManagedAssembly* ExistingAssembly = FindAssembly(*AssemblyName))
	{
		if (ExistingAssembly->IsAssemblyLoaded())
		{
			UE_LOGFMT(LogUnrealSharp, Display, "Assembly {0} is already loaded.", AssemblyName);
			return ExistingAssembly;
		}
	}
	
	UCSManagedAssembly* NewAssembly = NewObject<UCSManagedAssembly>(this, *AssemblyName);
	NewAssembly->InitializeManagedAssembly(AssemblyPath, bIsCollectible);
	LoadedAssemblies.Add(NewAssembly->GetAssemblyName(), NewAssembly);

	if (!NewAssembly->LoadManagedAssembly())
	{
		return nullptr;
	}

	UE_LOGFMT(LogUnrealSharp, Display, "Successfully loaded assembly at {0}.", AssemblyPath);
	return NewAssembly;
}

UCSManagedAssembly* UCSManager::LoadUserAssemblyByName(const FName AssemblyName, bool bIsCollectible)
{
	const FString AssemblyPath = FPaths::Combine(UnrealSharp::Paths::GetUserAssemblyDirectory(), AssemblyName.ToString() + TEXT(".dll"));
	return LoadAssemblyByPath(AssemblyPath, bIsCollectible);
}

UCSManagedAssembly* UCSManager::LoadPluginAssemblyByName(const FName AssemblyName, bool bIsCollectible)
{
	const FString AssemblyPath = FPaths::Combine(UnrealSharp::Paths::GetPluginAssembliesPath(), AssemblyName.ToString() + TEXT(".dll"));
	return LoadAssemblyByPath(AssemblyPath, bIsCollectible);
}

UCSManagedAssembly* UCSManager::FindOwningAssembly(UClass* Class)
{
	UClass* FirstNonBlueprintClass = FCSClassUtilities::GetFirstNonBlueprintClass(Class);
	
	if (ICSManagedTypeInterface* ManagedType = Cast<ICSManagedTypeInterface>(FirstNonBlueprintClass))
	{
		return ManagedType->GetOwningAssembly();
	}
	
	return FindOwningAssemblyGeneric<UClass, UBlueprintGeneratedClass>(FirstNonBlueprintClass);
}

UCSManagedAssembly* UCSManager::FindOwningAssembly(UScriptStruct* Struct)
{
	return FindOwningAssemblyGeneric<UScriptStruct, UUserDefinedStruct>(Struct);
}

UCSManagedAssembly* UCSManager::FindOwningAssembly(UEnum* Enum)
{
	return FindOwningAssemblyGeneric<UEnum, UUserDefinedEnum>(Enum);
}

UCSManagedAssembly* UCSManager::FindOwningAssemblySlow(UField* Field)
{
	const FCSFieldName ClassName(Field);

	for (auto& [Name, Assembly] : LoadedAssemblies)
	{
		TSharedPtr<FGCHandle> TypeHandle = Assembly->FindTypeHandle(ClassName);
		if (!TypeHandle.IsValid() || TypeHandle->IsNull())
		{
			continue;
		}

		const uint32 FieldID = Field->GetUniqueID();
		NativeClassToAssemblyMap.AddByHash(FieldID, FieldID, Assembly);
		return Assembly;
	}

	return nullptr;
}

FGCHandle UCSManager::FindManagedObject(const UObject* Object)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSManager::FindManagedObject);

	if (!IsValid(Object))
	{
		return FGCHandle::InvalidHandle();
	}

	const uint32 ObjectID = Object->GetUniqueID();
	if (TSharedPtr<FGCHandle>* FoundHandle = ManagedObjectHandles.FindByHash(ObjectID, ObjectID))
	{
		TSharedPtr<FGCHandle> HandlePtr = *FoundHandle;
		if (HandlePtr.IsValid() && !HandlePtr->IsNull())
		{
			return *HandlePtr;
		}
	}

	UCSManagedAssembly* OwningAssembly = FindOwningAssembly(Object->GetClass());
	if (!IsValid(OwningAssembly))
	{
		UE_LOGFMT(LogUnrealSharp, Error, "Failed to find assembly for {0}", Object->GetName());
		return FGCHandle::InvalidHandle();
	}

	return *OwningAssembly->CreateManagedObjectFromNative(Object);
}

FGCHandle UCSManager::FindOrCreateManagedInterfaceWrapper(UObject* Object, UClass* InterfaceClass)
{
	if (!Object->GetClass()->ImplementsInterface(InterfaceClass))
	{
		return FGCHandle::InvalidHandle();
	}

	UCSManagedAssembly* OwningAssembly = FindOwningAssembly(InterfaceClass);
	if (!IsValid(OwningAssembly))
	{
		UE_LOGFMT(LogUnrealSharp, Error, "Failed to find assembly for {0}", InterfaceClass->GetName());
		return FGCHandle::InvalidHandle();
	}
	
	TSharedPtr<FGCHandle> FoundHandle = OwningAssembly->GetOrCreateManagedInterface(Object, InterfaceClass);
	if (!FoundHandle.IsValid())
	{
		return FGCHandle::InvalidHandle();
	}

	return *FoundHandle;
}

void UCSManager::AddOrExecuteOnManagerInitialized(const FCSManagerInitializedEvent::FDelegate& Delegate)
{
	if (bIsInitialized)
	{
		Delegate.Execute(*this);
	}
	else
	{
		OnCSManagerInitialized.Add(Delegate);
	}
}