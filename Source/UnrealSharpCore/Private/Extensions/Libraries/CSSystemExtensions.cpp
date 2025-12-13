#include "Extensions/Libraries/CSSystemExtensions.h"
#include "Kismet/KismetSystemLibrary.h"

void UCSSystemExtensions::PrintStringInternal(UObject* WorldContextObject, const FString& InString, bool bPrintToScreen,
	bool bPrintToLog, FLinearColor TextColor, float Duration, const FName Key)
{
	UKismetSystemLibrary::PrintString(WorldContextObject, InString, bPrintToScreen, bPrintToLog, TextColor, Duration, Key);
}
