#include "CSManager.h"
#include "CSManagedGCHandle.h"
#include "CSAssembly.h"
#include "UnrealSharpCore.h"
#include "Export/FunctionsExporter.h"
#include "TypeGenerator/CSClass.h"
#include "TypeGenerator/Register/CSGeneratedClassBuilder.h"
#include "TypeGenerator/Register/CSTypeRegistry.h"
#include "Misc/Paths.h"
#include "Misc/App.h"
#include "UObject/Object.h"
#include "Misc/MessageDialog.h"
#include "Engine/Blueprint.h"
#include "UnrealSharpProcHelper/CSProcHelper.h"
#include <vector>

#include "Logging/StructuredLog.h"
#include "TypeGenerator/Factories/CSPropertyFactory.h"

#ifdef _WIN32
	#define PLATFORM_STRING(string) string
#elif defined(__unix__)
	#define PLATFORM_STRING(string) TCHAR_TO_ANSI(string)
#elif defined(__APPLE__)
	#define PLATFORM_STRING(string) TCHAR_TO_ANSI(string)
#endif

UCSManager* UCSManager::Instance = nullptr;

UCSManager& UCSManager::GetOrCreate()
{
	if (!Instance)
	{
		Instance = NewObject<UCSManager>(GetTransientPackage(), TEXT("CSManager"), RF_Public | RF_MarkAsRootSet);
		Instance->Initialize();
	}

	return *Instance;
}

UCSManager& UCSManager::Get()
{
	return *Instance;
}

void UCSManager::Shutdown()
{
	Instance = nullptr;
}

void UCSManager::Initialize()
{
#if WITH_EDITOR
	FString DotNetInstallationPath = FCSProcHelper::GetDotNetDirectory();
	if (DotNetInstallationPath.IsEmpty())
	{
		FString DialogText = FString::Printf(TEXT("UnrealSharp can't be initialized. An installation of .NET8 SDK can't be found on your system."));
		FMessageDialog::Open(EAppMsgType::Ok, FText::FromString(DialogText));
		return;
	}
	
	FString UnrealSharpLibraryPath = FCSProcHelper::GetUnrealSharpLibraryPath();
	if (!FPaths::FileExists(UnrealSharpLibraryPath))
	{
		FString FullPath = FPaths::ConvertRelativePathToFull(UnrealSharpLibraryPath);
		FString DialogText = FString::Printf(TEXT(
			"The bindings library could not be found at the following location:\n%s\n\n"
			"Right-click on the .uproject file and select \"Generate Visual Studio project files\" to compile.\n"
		), *FullPath);
		FMessageDialog::Open(EAppMsgType::Ok, FText::FromString(DialogText));
		return;
	}

	// Compile the C# project for any changes done outside the editor.
	if (!FApp::IsUnattended() && !FCSProcHelper::InvokeUnrealSharpBuildTool(BUILD_ACTION_BUILD_WEAVE))
	{
		Initialize();
		return;
	}
#endif

	// Remove this listener when the engine is shutting down.
	// Otherwise, we'll get a crash when the GC cleans up all the UObject.
	FCoreDelegates::OnPreExit.AddUObject(this, &UCSManager::OnEnginePreExit);
	
	//Create the package where we will store our generated types.
	UnrealSharpPackage = NewObject<UPackage>(nullptr, "/Script/UnrealSharp", RF_Public);
	UnrealSharpPackage->SetPackageFlags(PKG_CompiledIn);

	// Listen to GC callbacks.
	GUObjectArray.AddUObjectDeleteListener(this);

	// Initialize the C# runtime.
	if (!InitializeRuntime())
	{
		return;
	}

	FCSPropertyFactory::Initialize();

	// Try to load the user assembly, can be null when the project is first created.
	LoadAllUserAssemblies();
}

bool UCSManager::InitializeRuntime()
{
	if (!LoadRuntimeHost())
	{
		UE_LOG(LogUnrealSharp, Fatal, TEXT("Failed to load Runtime Host"));
		return false;
	}
	
	load_assembly_and_get_function_pointer_fn LoadAssemblyAndGetFunctionPointer;
	
#if WITH_EDITOR
	LoadAssemblyAndGetFunctionPointer = InitializeHostfxr();
#else
	LoadAssemblyAndGetFunctionPointer = InitializeHostfxrSelfContained();
#endif
	
	if (!LoadAssemblyAndGetFunctionPointer)
	{
		UE_LOG(LogUnrealSharp, Fatal, TEXT("Failed to initialize Runtime Host"));
		return false;
	}

	// Load assembly and get function pointer.
	FInitializeRuntimeHost InitializeUnrealSharp = nullptr;
	
	const FString EntryPointClassName = TEXT("UnrealSharp.Plugins.Main, UnrealSharp.Plugins");
	const FString EntryPointFunctionName = TEXT("InitializeUnrealSharp");

	const FString UnrealSharpLibraryAssembly = FPaths::ConvertRelativePathToFull(FCSProcHelper::GetUnrealSharpLibraryPath());
	
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
	if (!InitializeUnrealSharp(*UnrealSharpLibraryAssembly,
		&ManagedPluginsCallbacks,
		&FCSManagedCallbacks::ManagedCallbacks,
		(const void*)&UFunctionsExporter::StartExportingAPI))
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
	TArray<FString> UserAssemblies;
	FCSProcHelper::GetAllUserAssemblyPaths(UserAssemblies);

	if (UserAssemblies.IsEmpty())
	{
		return true;
	}

	for(const FString& UserAssembly : UserAssemblies)
	{
		if (!LoadAssembly(UserAssembly))
		{
			UE_LOG(LogUnrealSharp, Error, TEXT("Failed to load plugin %s!"), *UserAssembly);
			return false;
		}
	}

	return true;
}

load_assembly_and_get_function_pointer_fn UCSManager::InitializeHostfxr() const
{
	hostfxr_handle HostFXR_Handle = nullptr;
	FString RuntimeConfigPath =  FCSProcHelper::GetRuntimeConfigPath();

	if (!FPaths::FileExists(RuntimeConfigPath))
	{
		UE_LOG(LogUnrealSharp, Error, TEXT("No runtime config found"));
		return nullptr;
	}

	FString DotNetPath = FCSProcHelper::GetDotNetDirectory();
	FString RuntimeHostPath =  FCSProcHelper::GetRuntimeHostPath();

	UE_LOG(LogUnrealSharp, Log, TEXT("DotNetPath: %s"), *DotNetPath);
	UE_LOG(LogUnrealSharp, Log, TEXT("RuntimeHostPath: %s"), *RuntimeHostPath);

#if defined(_WIN32)
	hostfxr_initialize_parameters InitializeParameters;
	InitializeParameters.dotnet_root = PLATFORM_STRING(*DotNetPath);
	InitializeParameters.host_path = PLATFORM_STRING(*RuntimeHostPath);
	
	int32 ErrorCode = Hostfxr_Initialize_For_Runtime_Config(PLATFORM_STRING(*RuntimeConfigPath), &InitializeParameters, &HostFXR_Handle);
#else
	int32 ErrorCode = Hostfxr_Initialize_For_Runtime_Config(PLATFORM_STRING(*RuntimeConfigPath), nullptr, &HostFXR_Handle);
#endif
	
	if (ErrorCode != 0)
	{
		UE_LOG(LogUnrealSharp, Error, TEXT("hostfxr_initialize_for_runtime_config failed with code: %d"), ErrorCode);
		return nullptr;
	}

	void* LoadAssemblyAndGetFunctionPointer = nullptr;
	ErrorCode = Hostfxr_Get_Runtime_Delegate(HostFXR_Handle, hdt_load_assembly_and_get_function_pointer, &LoadAssemblyAndGetFunctionPointer);

	Hostfxr_Close(HostFXR_Handle);

	if (ErrorCode != 0 || LoadAssemblyAndGetFunctionPointer == nullptr)
	{
		UE_LOG(LogUnrealSharp, Error, TEXT("hostfxr_get_runtime_delegate failed with code: %d"), ErrorCode);
		return nullptr;
	}

#if defined(_WIN32)
	return static_cast<load_assembly_and_get_function_pointer_fn>(LoadAssemblyAndGetFunctionPointer);
#else
	return (load_assembly_and_get_function_pointer_fn)LoadAssemblyAndGetFunctionPointer;
#endif
}

load_assembly_and_get_function_pointer_fn UCSManager::InitializeHostfxrSelfContained() const
{
	FString MainAssemblyPath = FPaths::ConvertRelativePathToFull(FCSProcHelper::GetUnrealSharpLibraryPath());
	std::vector Args { PLATFORM_STRING(*MainAssemblyPath) };
	
	hostfxr_handle HostFXR_Handle = nullptr;
	FString DotNetPath = FCSProcHelper::GetAssembliesPath();
	FString RuntimeHostPath =  FCSProcHelper::GetRuntimeHostPath();

	hostfxr_initialize_parameters InitializeParameters;
	InitializeParameters.dotnet_root = PLATFORM_STRING(*DotNetPath);
	InitializeParameters.host_path = PLATFORM_STRING(*RuntimeHostPath);
	
#if defined(_WIN32)
	int ReturnCode = Hostfxr_Initialize_For_Dotnet_Command_Line(Args.size(), Args.data(), &InitializeParameters, &HostFXR_Handle);
#else
	int ReturnCode = Hostfxr_Initialize_For_Dotnet_Command_Line(Args.size(), const_cast<const char**>(Args.data()), &InitializeParameters, &HostFXR_Handle);
#endif
	
	if (ReturnCode != 0 || HostFXR_Handle == nullptr)
	{
		Hostfxr_Close(HostFXR_Handle);
	}

	void* Load_Assembly_And_Get_Function_Pointer = nullptr;
	ReturnCode = Hostfxr_Get_Runtime_Delegate(HostFXR_Handle, hdt_load_assembly_and_get_function_pointer, &Load_Assembly_And_Get_Function_Pointer);
	
	if (ReturnCode != 0 || Load_Assembly_And_Get_Function_Pointer == nullptr)
	{
		UE_LOG(LogUnrealSharp, Error, TEXT("hostfxr_get_runtime_delegate failed with code: %d"), ReturnCode);
	}

	Hostfxr_Close(HostFXR_Handle);

#if defined(_WIN32)
	return static_cast<load_assembly_and_get_function_pointer_fn>(Load_Assembly_And_Get_Function_Pointer);
#else
	return (load_assembly_and_get_function_pointer_fn)Load_Assembly_And_Get_Function_Pointer;
#endif
}

TSharedPtr<FCSAssembly> UCSManager::LoadAssembly(const FString& AssemblyPath)
{
	TSharedPtr<FCSAssembly> NewPlugin = MakeShared<FCSAssembly>(AssemblyPath);
	
	if (!NewPlugin->Load())
	{
		FText DialogText = FText::FromString(FString::Printf(TEXT("Failed to load Assembly with path: %s."), *AssemblyPath));
		FMessageDialog::Open(EAppMsgCategory::Error, EAppMsgType::Ok, DialogText);
		return nullptr;
	}
	
	LoadedPlugins.Add(*NewPlugin->GetAssemblyName(), NewPlugin);

	// Change from ManagedProjectName.dll > ManagedProjectName.metadata.json
	const FString MetadataPath = FPaths::ChangeExtension(AssemblyPath, "metadata.json");

	// Process the json file and register the types from the assembly.
	if (!FCSTypeRegistry::Get().ProcessMetaData(MetadataPath))
	{
		return nullptr;
	}
 
	UE_LOG(LogUnrealSharp, Display, TEXT("Successfully loaded Assembly with path %s."), *AssemblyPath);
	return NewPlugin;
}

bool UCSManager::UnloadAssembly(const FString& AssemblyName)
{
	TSharedPtr<FCSAssembly> Assembly;
	if (LoadedPlugins.RemoveAndCopyValue(*AssemblyName, Assembly))
	{
		return Assembly->Unload();
	}

	// If we can't find the Assembly, it's probably already unloaded.
	return true;
}

FGCHandle UCSManager::CreateNewManagedObject(UObject* Object, UClass* Class)
{
	ensureAlways(!UnmanagedToManagedMap.Contains(Object));

	UClass* ObjectClass = FCSGeneratedClassBuilder::GetFirstManagedClass(Class);
	
	if (!ObjectClass)
	{
		// If the class is not managed, we need to find the first native class.
		ObjectClass = FCSGeneratedClassBuilder::GetFirstNativeClass(Class);
	}
	
	TSharedRef<FCSharpClassInfo> ClassInfo = FCSTypeRegistry::Get().FindManagedType(ObjectClass);
	return CreateNewManagedObject(Object, ClassInfo->TypeHandle);
}

FGCHandle UCSManager::CreateNewManagedObject(UObject* Object, uint8* TypeHandle)
{
	FGCHandle NewManagedObject = FCSManagedCallbacks::ManagedCallbacks.CreateNewManagedObject(Object, TypeHandle);
	NewManagedObject.Type = GCHandleType::StrongHandle;

	if (NewManagedObject.IsNull())
	{
		// This should never happen. Potential issues: IL errors, typehandle is invalid.
		UE_LOG(LogUnrealSharp, Fatal, TEXT("Failed to create managed object for %s"), *Object->GetName());
		return FGCHandle();
	}
	
	return UnmanagedToManagedMap.Add(Object, NewManagedObject);
}

FGCHandle UCSManager::FindManagedObject(UObject* Object)
{
	if (!IsValid(Object))
	{
		RemoveManagedObject(Object);
		return FGCHandle();
	}

	if (FGCHandle* Handle = UnmanagedToManagedMap.Find(Object))
	{
		return *Handle;
	}
	
	return CreateNewManagedObject(Object, Object->GetClass());
}

void UCSManager::RemoveManagedObject(const UObjectBase* Object)
{
	FGCHandle Handle;
	if (UnmanagedToManagedMap.RemoveAndCopyValue(Object, Handle))
	{
		Handle.Dispose();
	}
}

void UCSManager::NotifyUObjectDeleted(const UObjectBase* Object, int32 Index)
{
	RemoveManagedObject(Object);
}

void UCSManager::OnUObjectArrayShutdown()
{
	GUObjectArray.RemoveUObjectDeleteListener(this);
}

void UCSManager::OnEnginePreExit()
{
	OnUObjectArrayShutdown();
}

uint8* UCSManager::GetTypeHandle(uint8* AssemblyHandle, const FString& Namespace, const FString& TypeName) const
{
	uint8* TypeHandle = FCSManagedCallbacks::ManagedCallbacks.LookupManagedType(AssemblyHandle, *Namespace, *TypeName);

	if (TypeHandle == nullptr)
	{
		// This should never happen. Something seriously wrong.
		UE_LOG(LogUnrealSharp, Fatal, TEXT("Couldn't find a TypeHandle for %s."), *TypeName);
		return nullptr;
	}

	return TypeHandle;
}

uint8* UCSManager::GetTypeHandle(const FString& AssemblyName, const FString& Namespace, const FString& TypeName) const
{
	const TSharedPtr<FCSAssembly> Plugin = LoadedPlugins.FindRef(*AssemblyName);

	if (!Plugin.IsValid() || !Plugin->IsAssemblyValid())
	{
		UE_LOG(LogUnrealSharp, Error, TEXT("Assembly is not valid."))
		return nullptr;
	}

	return GetTypeHandle(Plugin->GetAssemblyHandle().IntPtr, Namespace, TypeName);
}

uint8* UCSManager::GetTypeHandle(const FCSTypeReferenceMetaData& TypeMetaData) const
{
	return GetTypeHandle(TypeMetaData.AssemblyName.ToString(), TypeMetaData.Namespace.ToString(), TypeMetaData.Name.ToString());
}
