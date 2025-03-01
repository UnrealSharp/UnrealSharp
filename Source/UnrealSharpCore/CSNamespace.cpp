#include "CSNamespace.h"
#include "CSManager.h"

FCSNamespace::FCSNamespace(FName InNamespace): Namespace(InNamespace)
{
		
}

FCSNamespace FCSNamespace::Invalid()
{
	return FCSNamespace();
}

FString FCSNamespace::GetLastNamespace() const
{
	FString NamespaceString = Namespace.ToString();
	int32 LastDotIndex = NamespaceString.Find(TEXT("."), ESearchCase::CaseSensitive, ESearchDir::FromEnd);
		
	if (LastDotIndex == INDEX_NONE)
	{
		return NamespaceString;
	}
		
	return NamespaceString.Right(NamespaceString.Len() - LastDotIndex - 1);
}

bool FCSNamespace::GetParentNamespace(FCSNamespace& OutParent) const
{
	FString NamespaceString = Namespace.ToString();
	int32 LastDotIndex = NamespaceString.Find(".", ESearchCase::CaseSensitive, ESearchDir::FromEnd);
	
	if (LastDotIndex == INDEX_NONE)
	{
		return false;
	}
	
	FString ParentNamespace = NamespaceString.Left(LastDotIndex);
	OutParent = FCSNamespace(*ParentNamespace);
	return true;
}

UPackage* FCSNamespace::GetPackage() const
{
	return UCSManager::Get().FindManagedPackage(*this);
}

UPackage* FCSNamespace::TryGetAsNativePackage() const
{
	FString NativePackageName = FString::Printf(TEXT("/Script/%s"), *GetLastNamespace());
	return FindPackage(nullptr, *NativePackageName);
}

FName FCSNamespace::GetPackageName() const
{
	return *FString::Printf(TEXT("/Script/%s"), *Namespace.ToString());
}
