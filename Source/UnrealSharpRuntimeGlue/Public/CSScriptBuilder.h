#pragma once

class UNREALSHARPRUNTIMEGLUE_API FCSScriptBuilder
{
public:
	enum class IndentType
	{
		Spaces,
		Tabs
	};
	
	explicit FCSScriptBuilder(IndentType InIndentMode)
	: UnsafeBlockCount(0)
	, IndentCount(0)
	, IndentMode(InIndentMode)
	{
	}

	void Indent()
	{
		++IndentCount;
	}

	void Unindent()
	{
		--IndentCount;
	}

	void AppendLine()
	{
		if (Report.Len() != 0)
		{
			Report.Append(LINE_TERMINATOR);
		}

		if (IndentMode == IndentType::Spaces)
		{
			for (int32 Index = 0; Index < IndentCount; Index++)
			{
				Report.Append(TEXT("    "));
			}
		}
		else
		{
			for (int32 Index = 0; Index < IndentCount; Index++)
			{
				Report.Append(TEXT("\t"));
			}
		}
	}

	void Append(FStringView String)
	{
		Report.Append(String);
	}

	void Append(const FString& String)
	{
		Report.Append(String);
	}

	void Append(const TCHAR* String)
	{
		Report.Append(String);
	}

	void Append(const FName& Name)
	{
		Report.Append(Name.ToString());
	}

	void AppendLine(const FText& Text)
	{
		AppendLine();

		if (const FString* SourceString = FTextInspector::GetSourceString(Text))
		{
			Report.Append(*SourceString);
		}
		else
		{
			Report.Append(Text.ToString());
		}
	}

	void AppendLine(FStringView String)
	{
		AppendLine();
		Report.Append(String);
	}

	void AppendLine(const FString& String)
	{
		AppendLine();
		Report.Append(String);
	}

	void AppendLine(const ANSICHAR* Line)
	{
		AppendLine();
		Report.Append(Line);
	}

	void AppendLine(const FName& Name)
	{
		AppendLine();
		Report.Append(Name.ToString());
	}

	void AppendLine(const TCHAR* Line)
	{
		AppendLine();
		Report.Append(Line);
	}

	void OpenBrace()
	{
		AppendLine(TEXT("{"));
		Indent();
	}

	void CloseBrace()
	{
		Unindent();
		AppendLine(TEXT("}"));
	}

	void EndUnsafeBlock()
	{
		check(UnsafeBlockCount >= 0);
		if (!--UnsafeBlockCount)
		{
			CloseBrace();
		}
	}

	void Clear()
	{
		Report.Reset();
	}

	FText ToText() const
	{
		return FText::FromString(ToString());
	}

	FString ToString() const
	{
		return Report.ToString();
	}

	bool IsEmpty() const
	{
		return Report.Len() == 0;
	}

private:

	TStringBuilder<2048> Report;
	TArray<FString> Directives;
	int32 UnsafeBlockCount;
	int32 IndentCount;
	IndentType IndentMode;
	
};