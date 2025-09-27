#include "FTextExporter.h"

const TCHAR* UFTextExporter::ToString(FText* Text)
{
	if (!Text)
	{
		return nullptr;
	}
	
	return *Text->ToString();
}

void UFTextExporter::ToStringView(FText* Text, const TCHAR*& OutString, int32& OutLength)
{
    if (!Text)
    {
        OutString = nullptr;
        OutLength = 0;
        return;
    }

    const FString& AsString = Text->ToString();
    OutString = *AsString;
    OutLength = AsString.Len();
}

void UFTextExporter::FromString(FText* Text, const char* String)
{
	if (!Text)
	{
		return;
	}

	*Text = Text->FromString(String);
}

void UFTextExporter::FromStringView(FText* Text, const TCHAR* String, int32 Length)
{
    if (!Text)
    {
        return;
    }

    *Text = Text->FromStringView(FStringView(String, Length));
}

void UFTextExporter::FromName(FText* Text, FName Name)
{
	if (!Text)
	{
		return;
	}

	*Text = Text->FromName(Name);
}

void UFTextExporter::CreateEmptyText(FText* Text)
{
	if (!Text)
	{
		return;
	}

	*Text = FText::GetEmpty();
}

