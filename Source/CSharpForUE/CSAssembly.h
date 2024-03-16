#pragma once

#include "CSManagedGCHandle.h"

struct FCSManagedPluginCallbacks
{
	using LoadPluginCallback = GCHandleIntPtr(__stdcall*)(const TCHAR*);
	using UnloadPluginCallback = bool(__stdcall*)(const TCHAR*);
	
	LoadPluginCallback LoadPlugin = nullptr;
	UnloadPluginCallback UnloadPlugin = nullptr;
};

struct CSHARPFORUE_API FCSAssembly
{
	explicit FCSAssembly(const FString& InAssemblyPath)
		: AssemblyPath(InAssemblyPath)
		, AssemblyName(FPaths::GetBaseFilename(InAssemblyPath))
	{
	}

	bool Load();
	bool Unload() const;

	bool IsValid() const;
	const GCHandleIntPtr& GetHandle() const;

	const FString& GetAssemblyPath() { return AssemblyPath; }
	const FString& GetAssemblyName() { return AssemblyName; }

private:
	
	FGCHandle Assembly;
	FString AssemblyPath;
	FString AssemblyName;
	
};
