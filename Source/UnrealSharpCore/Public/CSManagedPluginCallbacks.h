#pragma once

#include "CSManagedGCHandle.h"

struct FCSManagedPluginCallbacks
{
	using LoadPluginCallback = FGCHandleIntPtr(__stdcall*)(const TCHAR*, bool);
	using UnloadPluginCallback = void(__stdcall*)(const TCHAR*);

	LoadPluginCallback LoadPlugin = nullptr;
	UnloadPluginCallback UnloadPlugin = nullptr;
};

inline FCSManagedPluginCallbacks& GetManagedPluginCallbacks() 
{
	static FCSManagedPluginCallbacks Instance;
	return Instance;
}
