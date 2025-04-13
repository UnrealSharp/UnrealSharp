﻿#include "FNameExporter.h"

void UFNameExporter::NameToString(FName Name, FString& OutString)
{
	Name.ToString(OutString);
}

void UFNameExporter::StringToName(FName& Name, const UTF16CHAR* String)
{
	Name = FName(String);
}

bool UFNameExporter::IsValid(FName Name)
{
	bool bIsValid = Name.IsValid();
	return bIsValid;
}
