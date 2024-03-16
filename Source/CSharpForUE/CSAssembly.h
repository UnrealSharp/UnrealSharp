#pragma once

#include "CSManagedGCHandle.h"

struct FCSManagedPluginCallbacks
{
	using LoadPluginCallback = GCHandleIntPtr(__stdcall*)(const TCHAR*);
	using UnloadPluginCallback = bool(__stdcall*)();
	
	LoadPluginCallback LoadPlugin = nullptr;
	UnloadPluginCallback UnloadPlugin = nullptr;
};

struct CSHARPFORUE_API FCSAssembly
{
	explicit FCSAssembly(const FString& InAssemblyPath) : AssemblyPath(InAssemblyPath)
	{
	}

	bool Load();
	bool Unload();

	bool IsAssemblyValid() const;
	GCHandleIntPtr GetAssemblyHandle() const;
	
	FGCHandle Assembly;
	FString AssemblyPath;
};
