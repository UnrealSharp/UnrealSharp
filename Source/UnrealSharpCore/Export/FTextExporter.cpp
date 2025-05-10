#include "FTextExporter.h"

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

	*Text = FText::GetEmpty();
}

