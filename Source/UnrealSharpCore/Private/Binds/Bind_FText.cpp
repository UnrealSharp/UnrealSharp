#include "CSBindsRegistry.h"

DECLARE_UNREALSHARP_BINDER(Bind_FText)
{
	const TCHAR* ToString(FText* Text)
	{
		if (!Text)
		{
			return nullptr;
		}
	
		return *Text->ToString();
	}

	void ToStringView(FText* Text, const TCHAR*& OutString, int32& OutLength)
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

	void FromString(FText* Text, const char* String)
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

	void FromStringView(FText* Text, const TCHAR* String, int32 Length)
	{
		if (!Text)
		{
			return;
		}

		*Text = Text->FromStringView(FStringView(String, Length));
	}

	void FromName(FText* Text, FName Name)
	{
		if (!Text)
		{
			return;
		}

		*Text = Text->FromName(Name);
	}

	void CreateEmptyText(FText* Text)
	{
		if (!Text)
		{
			return;
		}

		*Text = FText::GetEmpty();
	}

	bool IsCultureInvariant(FText* Text)
	{
		return Text->IsCultureInvariant();
	}

	bool IsFromStringTable(FText* Text)
	{
		return Text->IsFromStringTable();
	}

	bool IsInitializedFromString(FText* Text)
	{
		return Text->IsInitializedFromString();
	}

	bool IsNumeric(FText* Text)
	{
		return Text->IsNumeric();
	}

	bool IsEmpty(FText* Text)
	{
		return Text->IsEmpty();
	}
	
	BIND_UNREALSHARP_FUNCTION(ToString)
	BIND_UNREALSHARP_FUNCTION(ToStringView)
	BIND_UNREALSHARP_FUNCTION(FromString)
	BIND_UNREALSHARP_FUNCTION(FromStringView)
	BIND_UNREALSHARP_FUNCTION(FromName)
	BIND_UNREALSHARP_FUNCTION(CreateEmptyText)
	BIND_UNREALSHARP_FUNCTION(IsCultureInvariant)
	BIND_UNREALSHARP_FUNCTION(IsFromStringTable)
	BIND_UNREALSHARP_FUNCTION(IsInitializedFromString)
	BIND_UNREALSHARP_FUNCTION(IsNumeric)
	BIND_UNREALSHARP_FUNCTION(IsEmpty)
}
