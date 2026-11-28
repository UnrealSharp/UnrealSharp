#pragma once

#include "CoreMinimal.h"
#include "UserDefinedStructure/UserDefinedStructEditorData.h"
#include "CSScriptStructEditorData.generated.h"

/**
 * Editor data attached to a managed struct while it is rebuilt, so the editor's struct change listeners can snapshot it
 * like a user defined struct. Members and defaults come from C#: there are no variable descriptions, and a snapshot
 * takes its default values from the struct it duplicates.
 */
UCLASS(Transient)
class UCSScriptStructEditorData : public UUserDefinedStructEditorData
{
	GENERATED_BODY()
public:
	UCSScriptStructEditorData(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {}

	// UUserDefinedStructEditorDataBase interface
	virtual void ReinitializeDefaultInstance(FString* OutLog = nullptr) override;
	// End of UUserDefinedStructEditorDataBase interface
};
