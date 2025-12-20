#include "Export/FTextExporter.h"

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

	if (!String)
	{
		*Text = FText::GetEmpty();
		return;
	}

	// Contract: this API receives a UTF-8 byte string and must decode it into TCHAR.
	// NOTE: do NOT call this from C# by passing a string directly (the runtime will marshal it as ANSI and replace non-ASCII with '?').
	// Prefer FromStringView (TCHAR* + length) by pinning the UTF-16 buffer on the managed side to avoid data loss.
	*Text = FText::FromString(FString(UTF8_TO_TCHAR(String)));
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

