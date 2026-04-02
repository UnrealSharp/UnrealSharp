#include "Extensions/Libraries/CSTextExtensions.h"

FText UCSTextExtensions::Format(const FText& InPattern, const TArray<FText>& InArguments)
{
	FFormatOrderedArguments FormatArguments;
	FormatArguments.Reserve(InArguments.Num());
	
	for (const FText& Argument : InArguments)
	{
		FormatArguments.Emplace(Argument);
	}
	
	return FText::Format(InPattern, MoveTemp(FormatArguments));
}
