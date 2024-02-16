#include "FTextExporter.h"

void UFTextExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(ToString)
	EXPORT_FUNCTION(FromString)
	EXPORT_FUNCTION(FromName)
	EXPORT_FUNCTION(CreateEmptyText)
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
	check(true);
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
