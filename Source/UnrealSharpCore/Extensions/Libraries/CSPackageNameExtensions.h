#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "CSPackageNameExtensions.generated.h"

class UMovementComponent;

UCLASS(meta = (Internal))
class UCSPackageNameExtensions : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()

public:

	/**
	 * This will insert a mount point at the head of the search chain (so it can overlap an existing mount point and win).
	 *
	 * @param RootPath Logical Root Path.
	 * @param ContentPath Content Path on disk.
	 */
	UFUNCTION(meta = (ExtensionMethod, ScriptMethod))
	static void RegisterMountPoint(const FString& RootPath, const FString& ContentPath);
	
	/**
	 * This will remove a previously inserted mount point.
	 *
	 * @param RootPath Logical Root Path.
	 * @param ContentPath Content Path on disk.
	 */
	UFUNCTION(meta = (ExtensionMethod, ScriptMethod))
	static void UnRegisterMountPoint(const FString& RootPath, const FString& ContentPath);
	
	/**
	 * Returns whether the specific logical root path is a valid mount point.
	 */
	UFUNCTION(meta = (ExtensionMethod, ScriptMethod))
	static bool MountPointExists(const FString& RootPath);
};
