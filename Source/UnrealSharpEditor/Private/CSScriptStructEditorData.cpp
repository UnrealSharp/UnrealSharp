#include "CSScriptStructEditorData.h"

#include "StructUtils/UserDefinedStruct.h"

void UCSScriptStructEditorData::ReinitializeDefaultInstance(FString* OutLog)
{
	// A snapshot is created before its source struct is rebuilt, so both still share one layout.
	UUserDefinedStruct* Owner = GetOwnerStruct();
	const UUserDefinedStruct* Source = Owner->PrimaryStruct.Get();
	uint8* Defaults = const_cast<uint8*>(Owner->GetDefaultInstance());
	if (Defaults && Source && Source != Owner && Source->GetDefaultInstance() && Source->GetStructureSize() == Owner->GetStructureSize())
	{
		Owner->CopyScriptStruct(Defaults, Source->GetDefaultInstance());
	}

	Owner->UpdateStructFlags();
}
