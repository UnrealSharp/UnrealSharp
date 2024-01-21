// Fill out your copyright notice in the Description page of Project Settings.


#include "FTextExporter.h"

void UFTextExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(ToString)
	EXPORT_FUNCTION(FromString)
	EXPORT_FUNCTION(FromName)
	EXPORT_FUNCTION(CreateEmptyText)
	EXPORT_FUNCTION(Compare)
	EXPORT_FUNCTION(IsEmpty)
}

const TCHAR* UFTextExporter::ToString(FText* Text)
{
	if (!Text)
	{
		return nullptr;
	}
	
	return *Text->ToString();
}

void UFTextExporter::FromString(FText* Text, const char* String)
{
	if (!Text)
	{
		return;
	}

	*Text = Text->FromString(String);
}

void UFTextExporter::FromName(FText* Text, FName Name)
{
	if (!Text)
	{
		return;
	}

	Text->FromName(Name);
}

void UFTextExporter::CreateEmptyText(FText* Text)
{
	if (!Text)
	{
		return;
	}

	Text->GetEmpty();
}

bool UFTextExporter::Compare(FText* Text, FText* OtherText)
{
	if (!Text || !OtherText)
	{
		return false;
	}
	
	return Text->EqualTo(*OtherText);
}

bool UFTextExporter::IsEmpty(FText* Text)
{
	if (!Text)
	{
		return true;
	}

	return Text->IsEmpty();
}
