#pragma once

class FCSScriptBuilder;

struct FCSParsedTooltip
{
	struct FTokenString
	{
		FTokenString() = default;
		FTokenString(FTokenString&&) = default;
		FTokenString& operator=(FTokenString&&) = default;

		bool operator==(const FTokenString& InOther) const
		{
			return GetValue() == InOther.GetValue();
		}

		bool operator!=(const FTokenString& InOther) const
		{
			return GetValue() != InOther.GetValue();
		}

		FStringView GetValue() const
		{
			return SimpleValue.Len() > 0 ? SimpleValue : FStringView(ComplexValue);
		}

		void SetValue(FStringView InValue)
		{
			SimpleValue = InValue;
			ComplexValue.Reset();
		}

		void SetValue(FString&& InValue)
		{
			SimpleValue.Reset();
			ComplexValue = MoveTemp(InValue);
		}

		FStringView SimpleValue;
		FString ComplexValue;
	};

	struct FMiscToken
	{
		FMiscToken() = default;
		FMiscToken(FMiscToken&&) = default;
		FMiscToken& operator=(FMiscToken&&) = default;

		FTokenString TokenName;
		FTokenString TokenValue;
	};

	struct FParamToken
	{
		FParamToken() = default;
		FParamToken(FParamToken&&) = default;
		FParamToken& operator=(FParamToken&&) = default;

		FTokenString ParamName;
		FTokenString ParamType;
		FTokenString ParamComment;
	};

	typedef TArray<FMiscToken, TInlineAllocator<4>> FMiscTokensArray;
	typedef TArray<FParamToken, TInlineAllocator<8>> FParamTokensArray;

	int32 SourceTooltipLen = 0;

	FString BasicTooltipText;
	FMiscTokensArray MiscTokens;
	FParamTokensArray ParamTokens;
	FParamToken ReturnToken;
};

struct FCSTooltipParser
{
	static void ParseTooltip(FStringView InTooltip, FCSParsedTooltip& OutParsedTooltip);
	static void MakeCSharpTooltip(FCSScriptBuilder& Builder, const FText& TooltipText);
};
