#include "CSNamespace.h"
#include "CSManager.h"

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
	return UCSManager::Get().FindOrAddManagedPackage(*this);
}
