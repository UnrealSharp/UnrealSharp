#include "CSManager.h"
#include "CSManagedGCHandle.h"
#include "CSAssembly.h"
#include "CSDeveloperSettings.h"
#include "CSharpForUE.h"
#include "Export/FunctionsExporter.h"
#include "GlueGenerator/CSGenerator.h"
#include "Interfaces/IPluginManager.h"
#include "Misc/ScopedSlowTask.h"
#include "TypeGenerator/CSClass.h"
#include "TypeGenerator/Factories/CSPropertyFactory.h"
#include "TypeGenerator/Register/CSGeneratedClassBuilder.h"
#include "TypeGenerator/Register/CSMetaData.h"
#include "TypeGenerator/Register/CSTypeRegistry.h"
#include "Misc/Paths.h"
#include "Misc/App.h"
#include "CSDeveloperSettings.h"
#include "Misc/MessageDialog.h"

FUSScriptEngine* FCSManager::UnrealSharpScriptEngine = nullptr;
UPackage* FCSManager::UnrealSharpPackage = nullptr;

FString FCSManager::UserManagedProjectName = FString::Printf(TEXT("Managed%s"), FApp::GetProjectName());
FString FCSManager::PluginDirectory = FPaths::ConvertRelativePathToFull(IPluginManager::Get().FindPlugin(UE_PLUGIN_NAME)->GetBaseDir());
FString FCSManager::UnrealSharpDirectory = FPaths::Combine(PluginDirectory, "Managed", "UnrealSharp");
FString FCSManager::ScriptFolderDirectory = FPaths::ProjectDir() / "Script";
FString FCSManager::GeneratedClassesDirectory = FPaths::Combine(UnrealSharpDirectory, "UnrealSharp", "Generated");

void FCSManager::InitializeUnrealSharp()
{
	FString DotNetInstallationPath = GetDotNetDirectory();
	
	if (DotNetInstallationPath.IsEmpty())
	{
		FString DialogText = FString::Printf(TEXT("UnrealSharp can't be initialized. An installation of .NET SDK can't be found"));
		FMessageDialog::Open(EAppMsgType::Ok, FText::FromString(DialogText));
		return;
	}
	
	// Build the UnrealSharpBuildTool and the Weaver.
	// TODO: Make this a step in the build.cs instead
	if (!BuildPrograms())
	{
		UE_LOG(LogUnrealSharp, Fatal, TEXT("Failed to build program"));
		return;
	}
	
	// Check if the C# API is up to date.
	FCSGenerator::Get().StartGenerator(GeneratedClassesDirectory);

#if WITH_EDITOR
	// Make sure the C# API is up to date. This is only done in the editor.
	if (!BuildBindings())
	{
		UE_LOG(LogUnrealSharp, Fatal, TEXT("Failed to build bindings"));
		return;
	}
#endif
	
	// Generate the cs project. Ignore if it's already generated
	if (!GenerateProject())
	{
		InitializeUnrealSharp();
	 	return;
	}

	//Create the package where we will store our generated types.
	{
		UnrealSharpPackage = NewObject<UPackage>(nullptr, "/Script/UnrealSharp", RF_Public | RF_Standalone);
		UnrealSharpPackage->SetPackageFlags(PKG_CompiledIn);
	}

	// Listen to GC callbacks.
	{
		GUObjectArray.AddUObjectDeleteListener(this);
	}

	// Initialize the C# runtime.
	if (!InitializeBindings())
	{
		return;
	}

	// Initialize property factory before making the classes.
	FCSPropertyFactory::InitializePropertyFactory();

	// Try to load the user assembly, can be null when the project is first created.
	LoadUserAssembly();
}

bool FCSManager::InitializeBindings()
{
	if (!LoadRuntimeHost())
	{
		UE_LOG(LogUnrealSharp, Fatal, TEXT("Failed to load Runtime Host"));
		return false;
	}

	const auto LoadAssemblyAndGetFunctionPointer = InitializeRuntimeHost();
	
	if (!LoadAssemblyAndGetFunctionPointer)
	{
		UE_LOG(LogUnrealSharp, Fatal, TEXT("Failed to initialize Runtime Host"));
		return false;
	}

	// Load assembly and get function pointer.
	FInitializeRuntimeHost InitializeUnrealSharp = nullptr;
	
	const char_t* EntryPointClassName = TEXT("UnrealSharp.Plugins.Main, UnrealSharp.Plugins");
	const char_t* EntryPointFunctionName = TEXT("InitializeUnrealSharp");

	const FString UnrealSharpLibraryAssembly = FPaths::ConvertRelativePathToFull(GetUnrealSharpLibraryPath());
	
	const int32 ErrorCode = LoadAssemblyAndGetFunctionPointer(*UnrealSharpLibraryAssembly,
		EntryPointClassName,
		EntryPointFunctionName,
		UNMANAGEDCALLERSONLY_METHOD,
		nullptr,
		reinterpret_cast<void**>(&InitializeUnrealSharp));
	
	if (ErrorCode != 0)
	{
		UE_LOG(LogUnrealSharp, Fatal, TEXT("Tried to initialize UnrealSharp with error code: %d"), ErrorCode);
		return false;
	}

	// Entry point to C# to initialize UnrealSharp
	if (!InitializeUnrealSharp(*UnrealSharpLibraryAssembly, &ManagedPluginsCallbacks, &FCSManagedCallbacks::ManagedCallbacks, &UFunctionsExporter::StartExportingAPI))
	{
		UE_LOG(LogUnrealSharp, Fatal, TEXT("Failed to initialize UnrealSharp!"));
		return false;
	}
	
	return true;
}

bool FCSManager::LoadRuntimeHost()
{
	const FString RuntimeHostPath = GetRuntimeHostPath();

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

	void* DLLHandle;

	DLLHandle = FPlatformProcess::GetDllExport(RuntimeHost, TEXT("hostfxr_initialize_for_dotnet_command_line"));
	Hostfxr_Initialize_For_Dotnet_Command_Line = static_cast<hostfxr_initialize_for_dotnet_command_line_fn>(DLLHandle);

	DLLHandle = FPlatformProcess::GetDllExport(RuntimeHost, TEXT("hostfxr_initialize_for_runtime_config"));
	Hostfxr_Initialize_For_Runtime_Config = static_cast<hostfxr_initialize_for_runtime_config_fn>(DLLHandle);

	DLLHandle = FPlatformProcess::GetDllExport(RuntimeHost, TEXT("hostfxr_get_runtime_delegate"));
	Hostfxr_Get_Runtime_Delegate = static_cast<hostfxr_get_runtime_delegate_fn>(DLLHandle);

	DLLHandle = FPlatformProcess::GetDllExport(RuntimeHost, TEXT("hostfxr_close"));
	Hostfxr_Close = static_cast<hostfxr_close_fn>(DLLHandle);

	return Hostfxr_Initialize_For_Dotnet_Command_Line && Hostfxr_Get_Runtime_Delegate && Hostfxr_Close && Hostfxr_Initialize_For_Runtime_Config;
}

bool FCSManager::LoadUserAssembly()
{
	const FString UserAssemblyPath = GetUserAssemblyPath();

	if (!FPaths::FileExists(UserAssemblyPath))
	{
		UE_LOG(LogUnrealSharp, Error, TEXT("Couldn't find user assembly at %s"), *UserAssemblyPath);
		return false;
	}
	
	if (!LoadPlugin(UserAssemblyPath))
	{
		UE_LOG(LogUnrealSharp, Error, TEXT("Failed to load plugin %s!"), *UserAssemblyPath);
		return false;
	}

	return true;
}

load_assembly_and_get_function_pointer_fn FCSManager::InitializeRuntimeHost() const
{
	hostfxr_handle HostFXR_Handle = nullptr;
	FString RuntimeConfigPath = GetRuntimeConfigPath();

	if (!FPaths::FileExists(RuntimeConfigPath))
	{
		UE_LOG(LogUnrealSharp, Error, TEXT("No runtime config found"));
		return nullptr;
	}

	FString DotNetPath = GetDotNetDirectory();
	FString RuntimeHostPath = GetRuntimeHostPath();

	hostfxr_initialize_parameters InitializeParameters;
	InitializeParameters.dotnet_root = *DotNetPath;
	InitializeParameters.host_path = *RuntimeHostPath;
	
	int32 ErrorCode = Hostfxr_Initialize_For_Runtime_Config(*RuntimeConfigPath, &InitializeParameters, &HostFXR_Handle);
	
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

	return static_cast<load_assembly_and_get_function_pointer_fn>(LoadAssemblyAndGetFunctionPointer);
}

bool FCSManager::Build()
{
	return InvokeUnrealSharpBuildTool(EBuildAction::Build);
}

bool FCSManager::Clean()
{
	return InvokeUnrealSharpBuildTool(EBuildAction::Clean);
}

bool FCSManager::Rebuild()
{
	return InvokeUnrealSharpBuildTool(EBuildAction::Rebuild);
}
 
bool FCSManager::GenerateProject()
{
	return InvokeUnrealSharpBuildTool(EBuildAction::GenerateProject);
}

UPackage* FCSManager::GetUnrealSharpPackage()
{
	return UnrealSharpPackage;
}

FString FCSManager::GetRuntimeHostPath()
{
	FString DotNetPath = GetDotNetDirectory();
	FString RuntimeHostPath = FPaths::Combine(DotNetPath, "host/fxr", HOSTFXR_VERSION, HOSTFXR_WINDOWS);
	return RuntimeHostPath;
}

FString FCSManager::GetAssembliesPath()
{
	return FPaths::Combine(PluginDirectory, "Binaries", "DotNet", DOTNET_VERSION);
}

FString FCSManager::GetUnrealSharpLibraryPath()
{
	return GetAssembliesPath() / "UnrealSharp.Plugins.dll";
}

FString FCSManager::GetRuntimeConfigPath()
{
	return GetAssembliesPath() / "UnrealSharp.runtimeconfig.json";
}

FString FCSManager::GetUserAssemblyDirectory()
{
	return FPaths::Combine(FPaths::ProjectDir(), "Binaries", "UnrealSharp");
}

FString FCSManager::GetUserAssemblyPath()
{
	return FPaths::Combine(GetUserAssemblyDirectory(), UserManagedProjectName + ".dll");
}

FString FCSManager::GetManagedSourcePath()
{
	return FPaths::Combine(PluginDirectory, "Managed");
}

FString FCSManager::GetUnrealSharpBuildToolPath()
{
	return FPaths::ConvertRelativePathToFull(GetAssembliesPath() / "UnrealSharpBuildTool.exe");
}

TSharedPtr<FCSAssembly> FCSManager::LoadPlugin(const FString& AssemblyPath)
{
	TSharedPtr<FCSAssembly> NewPlugin = MakeShared<FCSAssembly>(AssemblyPath);
	
	if (!NewPlugin->Load())
	{
		FText DialogText = FText::FromString(FString::Printf(TEXT("Failed to load Assembly with path: %s."), *AssemblyPath));
		FMessageDialog::Open(EAppMsgCategory::Error, EAppMsgType::Ok, DialogText);
		return nullptr;
	}

	const FString PluginName = FPaths::GetBaseFilename(AssemblyPath);
	LoadedPlugins.Add(*PluginName, NewPlugin);

	// Change from ManagedProjectName.dll > ManagedProjectName.json
	const FString MetadataPath = FPaths::ChangeExtension(AssemblyPath, "json");

	// Process the json file and register the types.
	if (!FCSTypeRegistry::Get().ProcessMetaData(MetadataPath))
	{
		return nullptr;
	}
 
	UE_LOG(LogUnrealSharp, Display, TEXT("Successfully loaded Assembly with path %s."), *AssemblyPath);
	return NewPlugin;
}

bool FCSManager::UnloadPlugin(const FString& AssemblyName)
{
	TSharedPtr<FCSAssembly> Assembly;
	if (LoadedPlugins.RemoveAndCopyValue(*AssemblyName, Assembly))
	{
		return Assembly->Unload();
	}

	// If we can't find the Assembly, it's probably already unloaded.
	return true;
}

FGCHandle FCSManager::CreateNewManagedObject(UObject* Object, UClass* Class)
{
	ensureAlways(!UnmanagedToManagedMap.Contains(Object));

	UClass* ManagedClass = FCSGeneratedClassBuilder::GetFirstManagedClass(Class);
	const auto* ClassInfo = FCSTypeRegistry::Get().FindManagedType(ManagedClass ? ManagedClass : Class);
	
	return CreateNewManagedObject(Object, ClassInfo->TypeHandle);
}

FGCHandle FCSManager::CreateNewManagedObject(UObject* Object, uint8* TypeHandle)
{
	FGCHandle NewManagedObject = FCSManagedCallbacks::ManagedCallbacks.CreateNewManagedObject(Object, TypeHandle);
	NewManagedObject.Type = GCHandleType::StrongHandle;

	if (NewManagedObject.IsNull())
	{
		// This should never happen.
		UE_LOG(LogUnrealSharp, Fatal, TEXT("Failed to create managed object for %s"), *Object->GetName());
		return FGCHandle();
	}
	
	return UnmanagedToManagedMap.Add(Object, NewManagedObject);
}

FGCHandle FCSManager::FindManagedObject(UObject* Object)
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

void FCSManager::RemoveManagedObject(UObject* Object)
{
	FGCHandle Handle;
	if (UnmanagedToManagedMap.RemoveAndCopyValue(Object, Handle))
	{
		Handle.Dispose();
	}
}

uint8* FCSManager::GetTypeHandle(const FString& AssemblyName, const FString& Namespace, const FString& TypeName)
{
	const TSharedPtr<FCSAssembly> Plugin = LoadedPlugins.FindRef(*AssemblyName);

	if (!Plugin.IsValid() || !Plugin->IsAssemblyValid())
	{
		UE_LOG(LogUnrealSharp, Error, TEXT("Assembly is not valid."))
		return nullptr;
	}

	uint8* TypeHandle = FCSManagedCallbacks::ManagedCallbacks.LookupManagedType(Plugin->GetAssemblyHandle(), *Namespace, *TypeName);

	if (TypeHandle == nullptr)
	{
		// This should never happen. Something seriously wrong.
		UE_LOG(LogUnrealSharp, Fatal, TEXT("Couldn't find a TypeHandle for %s."), *TypeName);
		return nullptr;
	}

	return TypeHandle;
}

uint8* FCSManager::GetTypeHandle(const FTypeReferenceMetaData& TypeMetaData)
{
	return GetTypeHandle(TypeMetaData.AssemblyName, TypeMetaData.Namespace, TypeMetaData.Name);
}

void FCSManager::NotifyUObjectDeleted(const UObjectBase* ObjectBase, int32 Index)
{
	UObjectBase* NonConstObject = const_cast<UObjectBase*>(ObjectBase);
	UObject* Object = static_cast<UObject*>(NonConstObject);
	RemoveManagedObject(Object);
}

void FCSManager::OnUObjectArrayShutdown()
{
	GUObjectArray.RemoveUObjectDeleteListener(this);
}

bool FCSManager::BuildBindings()
{
	int32 ReturnCode = 0;
	FString Output;

	FString BuildConfiguration;
	GetDefault<UCSDeveloperSettings>()->GetBindingsBuildConfiguration(BuildConfiguration);

	FString Arguments = FString::Printf(TEXT("build -c %s"), *BuildConfiguration);
	return InvokeCommand(GetDotNetExecutablePath(), Arguments, ReturnCode, Output, &UnrealSharpDirectory);
}

bool FCSManager::BuildPrograms()
{
	FString DotNetPath = GetDotNetExecutablePath();
	FString UnrealSharpProgramsPath = FPaths::Combine(PluginDirectory, "Managed", "UnrealSharpPrograms");

	int32 ReturnCode = 0;
	FString Output;
	return InvokeCommand(DotNetPath, "build -c Release", ReturnCode, Output, &UnrealSharpProgramsPath);
}

bool FCSManager::InvokeUnrealSharpBuildTool(EBuildAction BuildAction)
{
	FName BuildActionCommand = StaticEnum<EBuildAction>()->GetNameByValue(BuildAction);

	FString BuildConfiguration;
	GetDefault<UCSDeveloperSettings>()->GetUserBuildConfiguration(BuildConfiguration);
	
	FString Args = FString::Printf(TEXT("--Action %s"), *BuildActionCommand.ToString());
	Args += FString::Printf(TEXT(" --BuildConfig %s"), *BuildConfiguration);
	Args += FString::Printf(TEXT(" --EngineDirectory \"%s\""), *FPaths::ConvertRelativePathToFull(FPaths::EngineDir()));
	Args += FString::Printf(TEXT(" --ProjectDirectory \"%s\""), *FPaths::ConvertRelativePathToFull(FPaths::ProjectDir()));
	Args += FString::Printf(TEXT(" --ProjectName %s"), FApp::GetProjectName());
	Args += FString::Printf(TEXT(" --PluginDirectory \"%s\""), *PluginDirectory);
	Args += FString::Printf(TEXT(" --OutputPath \"%s\""), *FPaths::ConvertRelativePathToFull(FPaths::Combine(FPaths::ProjectDir(), "Binaries", "UnrealSharp")));
	
	int32 ReturnCode = 0;
	FString Output;
	return InvokeCommand(GetUnrealSharpBuildToolPath(), Args, ReturnCode, Output);
}

bool FCSManager::InvokeCommand(const FString& ProgramPath, const FString& Arguments, int32& OutReturnCode, FString& Output, FString* InWorkingDirectory)
{
	double StartTime = FPlatformTime::Seconds();
	FString ProgramName = FPaths::GetBaseFilename(ProgramPath);
	
	if (!FPaths::FileExists(ProgramPath))
	{
		FText DialogText = FText::FromString(FString::Printf(TEXT("Failed to find %s at %s"), *ProgramPath, *ProgramName));
		FMessageDialog::Open(EAppMsgType::Ok, DialogText);
		return false;
	}
		
	const bool bLaunchDetached = false;
	const bool bLaunchHidden = true;
	const bool bLaunchReallyHidden = bLaunchHidden;
	
	void* ReadPipe;
	void* WritePipe;
	FPlatformProcess::CreatePipe(ReadPipe, WritePipe);

	FString WorkingDirectory = InWorkingDirectory ? *InWorkingDirectory : FPaths::GetPath(ProgramPath);
	
	FProcHandle ProcHandle = FPlatformProcess::CreateProc(*ProgramPath,
														  *Arguments,
														  bLaunchDetached,
														  bLaunchHidden,
														  bLaunchReallyHidden,
														  NULL, 0, *WorkingDirectory, WritePipe, ReadPipe);

	if (!ProcHandle.IsValid())
	{
		FString DialogText = FString::Printf(TEXT("%s failed to launch!"), *ProgramName);
		FMessageDialog::Open(EAppMsgType::Ok, FText::FromString(DialogText));
		return false;
	}
	
	while (FPlatformProcess::IsProcRunning(ProcHandle))
	{
		Output += FPlatformProcess::ReadPipe(ReadPipe);
	}
	
	FPlatformProcess::GetProcReturnCode(ProcHandle, &OutReturnCode);
	FPlatformProcess::CloseProc(ProcHandle);
	FPlatformProcess::ClosePipe(ReadPipe, WritePipe);

	if (OutReturnCode != 0)
	{
		UE_LOG(LogUnrealSharp, Warning, TEXT("%s task failed (Args: %s) with return code %d"), *ProgramName, *Arguments, OutReturnCode)
		
		FText DialogText = FText::FromString(FString::Printf(TEXT("%s task failed: \n %s"), *ProgramName, *Output));
		FMessageDialog::Open(EAppMsgType::Ok, DialogText);
		return false;
	}

	double EndTime = FPlatformTime::Seconds();
	double ElapsedTime = (EndTime - StartTime);
	UE_LOG(LogUnrealSharp, Log, TEXT("%s with args (%s) took %f seconds to execute."), *ProgramName, *Arguments, ElapsedTime);
	
	return true;
}

FString FCSManager::GetDotNetDirectory()
{
	const FString PathVariable = FPlatformMisc::GetEnvironmentVariable(TEXT("PATH"));
		
	TArray<FString> Paths;
	PathVariable.ParseIntoArray(Paths, FPlatformMisc::GetPathVarDelimiter());

	for (FString& Path : Paths)
	{
		if (!Path.Contains(TEXT("dotnet")))
		{
			continue;
		}
		
		if (!FPaths::DirectoryExists(Path))
		{
			UE_LOG(LogUnrealSharp, Warning, TEXT("Found path to DotNet, but the directory doesn't exist: %s"), *Path);
			break;
		}
			
		return Path;
	}
    
	return "";
}

FString FCSManager::GetDotNetExecutablePath()
{
	return GetDotNetDirectory() + "dotnet.exe";
}

#define LOCTEXT_NAMESPACE "CSManager"


#undef LOCTEXT_NAMESPACE
