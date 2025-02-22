#pragma once

struct FCSNamespace
{
	FCSNamespace(FName InNamespace = FName());
	
	FName GetFullNamespace() const { return Namespace; }
	FString GetFullNamespaceString() const { return Namespace.ToString(); }
	FString GetThisNamespace() const;

	bool GetParent(FCSNamespace& OutParent) const;
	
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

