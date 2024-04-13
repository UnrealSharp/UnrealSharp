#include "CSTooltipParser.h"
#include "CSScriptBuilder.h"

void FCSTooltipParser::ParseTooltip(FStringView InTooltip, FCSParsedTooltip& OutParsedTooltip)
{
	const FStringView SourceTooltip = InTooltip;
	int32 SourceTooltipParseIndex = 0;
	
	OutParsedTooltip.SourceTooltipLen = InTooltip.Len();
	OutParsedTooltip.BasicTooltipText.Reserve(SourceTooltip.Len());

	auto SkipToNextToken = [&SourceTooltip, &SourceTooltipParseIndex]()
	{
		while (SourceTooltipParseIndex < SourceTooltip.Len() && (FChar::IsWhitespace(SourceTooltip[SourceTooltipParseIndex]) || SourceTooltip[SourceTooltipParseIndex] == TEXT('-')))
		{
			++SourceTooltipParseIndex;
		}
	};

	auto ParseSimpleToken = [&SourceTooltip, &SourceTooltipParseIndex](FCSParsedTooltip::FTokenString& OutToken)
	{
		const int32 TokenStartIndex = SourceTooltipParseIndex;
		while (SourceTooltipParseIndex < SourceTooltip.Len() && !FChar::IsWhitespace(SourceTooltip[SourceTooltipParseIndex]))
		{
			++SourceTooltipParseIndex;
		}
		OutToken.SimpleValue = SourceTooltip.Mid(TokenStartIndex, SourceTooltipParseIndex - TokenStartIndex);
	};

	auto ParseComplexToken = [&SourceTooltip, &SourceTooltipParseIndex](FCSParsedTooltip::FTokenString& OutToken)
	{
		int32 TokenStartIndex = SourceTooltipParseIndex;
		while (SourceTooltipParseIndex < SourceTooltip.Len() && SourceTooltip[SourceTooltipParseIndex] != TEXT('@'))
		{
			// Convert a new-line within a token to a space
			if (FChar::IsLinebreak(SourceTooltip[SourceTooltipParseIndex]))
			{
				// Can no longer process this as a simple token - copy what we've parsed so far and reset...
				if (TokenStartIndex != INDEX_NONE)
				{
					OutToken.ComplexValue = SourceTooltip.Mid(TokenStartIndex, SourceTooltipParseIndex - TokenStartIndex);
					TokenStartIndex = INDEX_NONE;
				}

				while (SourceTooltipParseIndex < SourceTooltip.Len() && FChar::IsLinebreak(SourceTooltip[SourceTooltipParseIndex]))
				{
					++SourceTooltipParseIndex;
				}

				while (SourceTooltipParseIndex < SourceTooltip.Len() && FChar::IsWhitespace(SourceTooltip[SourceTooltipParseIndex]))
				{
					++SourceTooltipParseIndex;
				}

				OutToken.ComplexValue += TEXT(' ');
			}

			// Sanity check in case the first character after the new-line is @
			if (SourceTooltipParseIndex < SourceTooltip.Len() && SourceTooltip[SourceTooltipParseIndex] != TEXT('@'))
			{
				if (TokenStartIndex == INDEX_NONE)
				{
					OutToken.ComplexValue += SourceTooltip[SourceTooltipParseIndex];
				}
				++SourceTooltipParseIndex;
			}
		}
		if (TokenStartIndex == INDEX_NONE)
		{
			OutToken.ComplexValue.TrimEndInline();
		}
		else
		{
			OutToken.SimpleValue = SourceTooltip.Mid(TokenStartIndex, SourceTooltipParseIndex - TokenStartIndex);
			OutToken.SimpleValue.TrimEndInline();
		}
	};
	
	while (SourceTooltipParseIndex < SourceTooltip.Len())
	{
		if (SourceTooltip[SourceTooltipParseIndex] == TEXT('@'))
		{
			++SourceTooltipParseIndex; // Walk over the @
			if (SourceTooltip[SourceTooltipParseIndex] == TEXT('@'))
			{
				// Literal @ character
				OutParsedTooltip.BasicTooltipText += TEXT('@');
				continue;
			}

			// Parse out the token name
			FCSParsedTooltip::FTokenString TokenName;
			SkipToNextToken();
			ParseSimpleToken(TokenName);

			if (TokenName.GetValue() == TEXT("param"))
			{
				FCSParsedTooltip::FParamToken& ParamToken = OutParsedTooltip.ParamTokens.AddDefaulted_GetRef();

				// Parse out the parameter name
				SkipToNextToken();
				ParseSimpleToken(ParamToken.ParamName);

				// Parse out the parameter comment
				SkipToNextToken();
				ParseComplexToken(ParamToken.ParamComment);
			}
			else if (TokenName.GetValue() == TEXT("return") || TokenName.GetValue() == TEXT("returns"))
			{
				// Parse out the return value token
				SkipToNextToken();
				ParseComplexToken(OutParsedTooltip.ReturnToken.ParamComment);
			}
			else
			{
				FCSParsedTooltip::FMiscToken& MiscToken = OutParsedTooltip.MiscTokens.AddDefaulted_GetRef();
				MiscToken.TokenName = MoveTemp(TokenName);

				// Parse out the token value
				SkipToNextToken();
				ParseComplexToken(MiscToken.TokenValue);
			}
		}
		else
		{
			// Normal character
			OutParsedTooltip.BasicTooltipText += SourceTooltip[SourceTooltipParseIndex++];
		}
	}
}

void FCSTooltipParser::MakeCSharpTooltip(FCSScriptBuilder& Builder, const FText& TooltipText)
{
	const FString* Tooltip = FTextInspector::GetSourceString(TooltipText);

	if (!Tooltip || Tooltip->IsEmpty())
	{
		return;
	}
	
	FCSParsedTooltip ParsedTooltip;
	ParseTooltip(*Tooltip, ParsedTooltip);
	
	if (!ParsedTooltip.BasicTooltipText.IsEmpty())
	{
		TArray<FString> Lines;
		ParsedTooltip.BasicTooltipText.ParseIntoArray(Lines, TEXT("\n"), true);
		
		Builder.AppendLine(TEXT("/// <summary>"));
		for (const FString& Line : Lines)
		{
			Builder.AppendLine(FString::Printf(TEXT("/// %s"), *Line));
		}
		Builder.AppendLine(TEXT("/// </summary>"));
	}

	if (!ParsedTooltip.ParamTokens.IsEmpty())
	{
		for (const FCSParsedTooltip::FParamToken& MiscToken : ParsedTooltip.ParamTokens)
		{
			Builder.AppendLine(TEXT("/// <param name=\""));
			Builder.Append(MiscToken.ParamName.GetValue());
			Builder.Append(TEXT("\">"));
			Builder.Append(MiscToken.ParamComment.GetValue());
			Builder.Append(TEXT("</param>"));
		}
	}

	if (!ParsedTooltip.ReturnToken.ParamComment.GetValue().IsEmpty())
	{
		Builder.AppendLine(TEXT("/// <returns>"));
		Builder.Append(ParsedTooltip.ReturnToken.ParamComment.GetValue());
		Builder.Append(TEXT("</returns>"));
	}
}
