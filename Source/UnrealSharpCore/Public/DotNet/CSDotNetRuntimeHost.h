#pragma once

#include "CoreMinimal.h"

#include <coreclr_delegates.h>
#include <hostfxr.h>

#include "HAL/PlatformProcess.h"
#include "CSInteropTypeTraits.h"

struct FCSManagedCallbacks;
struct FCSManagedPluginCallbacks;

/**
 * Result of the managed InitializeUnrealSharp entry point. Mirrors UnrealSharp.Plugins.FCSInitializationResult:
 * on failure, managed code writes the exception text into Message as a null-terminated UTF-8 string.
 */
struct FCSInitializationResult
{
	static constexpr int32 MessageCapacity = 4096;

	bool bSuccess = false;
	UTF8CHAR Message[MessageCapacity] = {};
};

static_assert(sizeof(FCSInitializationResult) == 1 + FCSInitializationResult::MessageCapacity, "FCSInitializationResult must match the managed layout.");
CS_ASSERT_INTEROP_SAFE_TYPE(FCSInitializationResult);

using FInitializeUnrealSharp = void (*)(const UTF8CHAR*, FCSManagedPluginCallbacks*, const void*, FCSManagedCallbacks*, FCSInitializationResult*);
CS_ASSERT_INTEROP_SAFE_FUNCTION(FInitializeUnrealSharp);

static_assert(sizeof(TCHAR) == sizeof(char16_t), "TCHAR must be a 16-bit character.");

struct FCSDotNetLayout
{
	FString DotNetRoot;
	FString HostFxrPath;
	FString AppAssemblyPath;
	FString RuntimeConfigPath;
	bool bSelfContained = false;

	bool IsValid() const
	{
		return !DotNetRoot.IsEmpty() && !HostFxrPath.IsEmpty() && !RuntimeConfigPath.IsEmpty();
	}
};

class FCSDotNetRuntimeHost
{
public:
	FCSDotNetRuntimeHost() = default;
	~FCSDotNetRuntimeHost();

	bool InitializeManagedRuntime();
	void ShutdownManagedRuntime();

private:
	static FCSDotNetLayout ResolveDotNetLayout(const FString& PluginAssemblyPath);

	load_assembly_and_get_function_pointer_fn InitializeHost();
	load_assembly_and_get_function_pointer_fn ConfigureRuntime(const FCSDotNetLayout& Layout) const;

	template <typename FunctionPointer>
	bool BindExport(FunctionPointer& OutFunctionPointer, const TCHAR* ExportName)
	{
		OutFunctionPointer = reinterpret_cast<FunctionPointer>(FPlatformProcess::GetDllExport(RuntimeHost, ExportName));
		return OutFunctionPointer != nullptr;
	}

	hostfxr_initialize_for_dotnet_command_line_fn Hostfxr_InitForCommandLine = nullptr;
	hostfxr_initialize_for_runtime_config_fn Hostfxr_InitForRuntimeConfig = nullptr;
	hostfxr_get_runtime_delegate_fn Hostfxr_GetRuntimeDelegate = nullptr;
	hostfxr_close_fn Hostfxr_Close = nullptr;

	void* RuntimeHost = nullptr;
};
