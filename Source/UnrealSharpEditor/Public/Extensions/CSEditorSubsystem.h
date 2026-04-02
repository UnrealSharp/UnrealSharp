#pragma once

#include "CoreMinimal.h"
#include "EditorSubsystem.h"
#include "CSEditorSubsystem.generated.h"

UCLASS(Blueprintable, BlueprintType, Abstract)
class UCSEditorSubsystem : public UEditorSubsystem
{
	GENERATED_BODY()

	// USubsystem Begin
	virtual void Initialize(FSubsystemCollectionBase& Collection) override;
	virtual void Deinitialize() override;
	virtual bool ShouldCreateSubsystem(UObject* Outer) const override;
	// End

protected:

	UFUNCTION(BlueprintNativeEvent, meta = (ScriptName = "ShouldCreateSubsystem"))
	bool K2_ShouldCreateSubsystem(UObject* SubsystemOuter) const;
  
	UFUNCTION(BlueprintImplementableEvent, meta = (ScriptName = "Initialize"))
	void K2_Initialize();
  
	UFUNCTION(BlueprintImplementableEvent, meta = (ScriptName = "Deinitialize"))
	void K2_Deinitialize();
};
