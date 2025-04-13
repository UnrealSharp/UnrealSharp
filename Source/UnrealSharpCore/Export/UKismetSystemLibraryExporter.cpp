#include "UKismetSystemLibraryExporter.h"
#include "Kismet/KismetSystemLibrary.h"

void UUKismetSystemLibraryExporter::PrintString(const UObject* WorldContextObject, const UTF16CHAR* Message, float Duration, FLinearColor Color, bool PrintToScreen, bool PrintToConsole, const FName Key)
{
	UKismetSystemLibrary::PrintString(WorldContextObject, Message, PrintToScreen, PrintToConsole, Color, Duration, Key);
}
