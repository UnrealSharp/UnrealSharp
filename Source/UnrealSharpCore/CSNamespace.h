#pragma once

struct FCSNamespace
{
	FCSNamespace(FName InNamespace = NAME_None);

	// Get the namespace as a FName
	FName GetFName() const { return Namespace; }

	// Get the namespace as a string
	FString GetName() const { return Namespace.ToString(); }

	// Gets the name of the last part of the namespace. For example, if the namespace is "UnrealSharp.Core", this will return "Core".
	FString GetLastNamespace() const;

	bool GetParentNamespace(FCSNamespace& OutParent) const;
	bool IsValid() const { return Namespace != NAME_None; }

	UPackage* GetPackage() const;
	UPackage* TryGetAsNativePackage() const;
	
	FName GetPackageName() const;

	static FCSNamespace Invalid();

	bool operator == (const FCSNamespace& Other) const
	{
		return Namespace == Other.Namespace;
	}

	friend uint32 GetTypeHash(const FCSNamespace& Namespace)
	{
		return GetTypeHash(Namespace.Namespace);
	}

private:
	FName Namespace;
};

