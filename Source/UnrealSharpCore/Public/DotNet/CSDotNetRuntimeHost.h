#pragma once

#include <coreclr_delegates.h>
#include <hostfxr.h>
#include "HAL/PlatformProcess.h"

struct FCSManagedCallbacks;
struct FCSManagedPluginCallbacks;

using FInitializeRuntimeHost = bool (*)(const TCHAR*, const TCHAR*, FCSManagedPluginCallbacks*, const void*, FCSManagedCallbacks*);

class FCSDotNetRuntimeHost
{
public:
	FCSDotNetRuntimeHost() = default;
	~FCSDotNetRuntimeHost();
	
	bool InitializeManagedRuntime();

private:
	load_assembly_and_get_function_pointer_fn InitializeHost();
	load_assembly_and_get_function_pointer_fn ConfigureRuntime() const;
	
	template <typename TFn>
	bool BindExport(TFn& OutPtr, const TCHAR* ExportName)
	{
		OutPtr = reinterpret_cast<TFn>(FPlatformProcess::GetDllExport(RuntimeHost, ExportName));
		return OutPtr != nullptr;
	}

	hostfxr_initialize_for_dotnet_command_line_fn Hostfxr_InitForCommandLine = nullptr;
	hostfxr_initialize_for_runtime_config_fn Hostfxr_InitForRuntimeConfig = nullptr;
	hostfxr_get_runtime_delegate_fn Hostfxr_GetRuntimeDelegate = nullptr;
	hostfxr_close_fn Hostfxr_Close = nullptr;

	void* RuntimeHost = nullptr;
};
