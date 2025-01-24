#include "CSPackageNameExtensions.h"

void UCSPackageNameExtensions::RegisterMountPoint(const FString& RootPath, const FString& ContentPath)
{
	FPackageName::RegisterMountPoint(RootPath, ContentPath);
}

void UCSPackageNameExtensions::UnRegisterMountPoint(const FString& RootPath, const FString& ContentPath)
{
	FPackageName::UnRegisterMountPoint(RootPath, ContentPath);
}

bool UCSPackageNameExtensions::MountPointExists(const FString& RootPath)
{
	return FPackageName::MountPointExists(RootPath);
}
