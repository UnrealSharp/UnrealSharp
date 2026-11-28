#pragma once

#include "StructUtils/UserDefinedStruct.h"
#include "CSManagedTypeInterface.h"
#include "CSScriptStruct.generated.h"

UCLASS()
class UCSScriptStruct : public UUserDefinedStruct, public ICSManagedTypeInterface
{
	GENERATED_BODY()
public:
	
	// UObject interface
	virtual bool IsFullNameStableForNetworking() const override { return true; }
	virtual bool IsNameStableForNetworking() const override { return true; }
	// End of UObject interface
	
	void Initialize();

#if WITH_EDITOR
	/**
	 * Moves everything holding the current layout onto a snapshot of it and notifies the editor that the struct is about
	 * to change. Returns the managed structs embedding this struct, which must be rebuilt once it has been.
	 */
	void PrepareForRecompile(TArray<TSharedPtr<FCSManagedTypeDefinition>>& OutManagedOwners);

	/** Notifies the editor that the struct has been rebuilt, pairing the notification sent by PrepareForRecompile. */
	void FinishRecompile();

	/**
	 * Editor data class (a UUserDefinedStructEditorData) attached while the struct is rebuilt. The editor's struct change
	 * listeners snapshot user defined structs through it, so notifications are only sent when the editor module set it.
	 */
	static UNREALSHARPCORE_API UClass* SnapshotEditorDataClass;
#endif
	
private:
	void InitializeStructDefaults();
	TUniquePtr<uint8[]> StructDefaults;

#if WITH_EDITOR
	bool bPendingPostChange = false;
#endif
};
