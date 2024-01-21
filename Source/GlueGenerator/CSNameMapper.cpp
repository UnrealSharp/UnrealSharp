#include "CSNameMapper.h"
#include "CSModule.h"
#include "CSGenerator.h"
#include "UObject/Class.h"

FString FCSNameMapper::GetQualifiedName(const UClass* Class) const
{
	const FCSModule& BindingsModule = ScriptGenerator->FindModule(Class);
	return FString::Printf(TEXT("%s.%s"), *BindingsModule.GetNamespace(), *GetScriptClassName(Class));
}

FString FCSNameMapper::GetQualifiedName(const UScriptStruct* Struct) const
{
	const FCSModule& BindingsModule = ScriptGenerator->FindModule(Struct);
	return FString::Printf(TEXT("%s.%s"), *BindingsModule.GetNamespace(), *GetStructScriptName(Struct));
}

FString FCSNameMapper::GetQualifiedName(const UEnum* Enum) const
{
	const FCSModule& BindingsModule = ScriptGenerator->FindModule(Enum->GetPackage());
	return FString::Printf(TEXT("%s.%s"), *BindingsModule.GetNamespace(), *Enum->GetName());
}

FString EscapeKeywords(const FString& InName)
{
	static TSet<FString, FCaseSensitiveStringSetFuncs> CSharpKeywords{
		TEXT("abstract"), TEXT("as"), TEXT("base"), TEXT("bool"),
		TEXT("break"), TEXT("byte"), TEXT("case"), TEXT("catch"),
		TEXT("char"), TEXT("checked"), TEXT("class"), TEXT("const"),
		TEXT("continue"), TEXT("decimal"), TEXT("default"), TEXT("delegate"),
		TEXT("do"), TEXT("double"), TEXT("else"), TEXT("enum"),
		TEXT("event"), TEXT("explicit"), TEXT("extern"), TEXT("false"),
		TEXT("finally"), TEXT("fixed"), TEXT("float"), TEXT("for"),
		TEXT("foreach"), TEXT("goto"), TEXT("if"), TEXT("implicit"),
		TEXT("in"), TEXT("int"), TEXT("interface"), TEXT("internal"),
		TEXT("is"), TEXT("lock"), TEXT("long"), TEXT("namespace"),
		TEXT("new"), TEXT("null"), TEXT("object"), TEXT("operator"),
		TEXT("out"), TEXT("override"), TEXT("params"), TEXT("private"),
		TEXT("protected"), TEXT("public"), TEXT("readonly"), TEXT("ref"),
		TEXT("return"), TEXT("sbyte"), TEXT("sealed"), TEXT("short"),
		TEXT("sizeof"), TEXT("stackalloc"), TEXT("static"), TEXT("string"),
		TEXT("struct"), TEXT("switch"), TEXT("this"), TEXT("throw"),
		TEXT("true"), TEXT("try"), TEXT("typeof"), TEXT("uint"),
		TEXT("ulong"), TEXT("unchecked"), TEXT("unsafe"), TEXT("ushort"),
		TEXT("using"), TEXT("virtual"), TEXT("void"), TEXT("volatile"),
		TEXT("while")
	};

	if (CSharpKeywords.Contains(InName))
	{
		return TEXT("@") + InName;
	}
	return InName;
}

// quick and dirty SNAKE_CASE to PascalCase/camelCase conversion
FString DeSnakifyName(const FString &SnakeCaseString, bool uppercaseFirst = false)
{
	// Fetermine whether the string has lowercase and uppercase char, and whether it's "mixed".
	// We consider "mixed" to mean it has an uppercase character followed by a lowercase.
	// This means we don't consider e.g. RGBA16f to be mixed, but we do consider MyThing to be mixed.
	// We preserve case on mixed case values, other than uppercasing word starts.
	// goals:
	//    MY_THING -> MyThing
	//    MyThing -> MyThing
	//    RGBA16f -> Rgba16f
	bool hasLower = false;
	bool hasUpper = false;
	bool lastWasUpper = false;
	bool isMixed = false;
	for (auto ch : SnakeCaseString)
	{
		if (FChar::IsLower(ch))
		{
			hasLower = true;
			if (lastWasUpper)
			{
				isMixed = true;
			}
			lastWasUpper = false;
		}
		else if (FChar::IsUpper(ch))
		{
			hasUpper = true;
			lastWasUpper = true;
		}
		else
		{
			lastWasUpper = false;
		}
	}

	FString PascalCaseString = FString();

	bool uppercaseNext = uppercaseFirst;
	int len = SnakeCaseString.Len();
	int uppercaseCount;
	for (int i = 0; i < len; i++)
	{
		auto ch = SnakeCaseString[i];

		//skip underscores. first char and char after every underscore or digit should be uppercase
		if (FChar::IsUnderscore (ch))
		{
			uppercaseCount = 0;
			uppercaseNext = true;
			continue;
		}

		if (FChar::IsDigit(ch))
		{
			//don't uppercase trailing letters e.g. RGBA16f
			bool precedesPenultimate = i + 2 < len;
			if (precedesPenultimate)
			{
				uppercaseNext = true;
			}
			uppercaseCount = 0;
		}
		else if (uppercaseNext)
		{
			uppercaseNext = false;
			if (hasLower)
			{
				ch = FChar::ToUpper(ch);
			}
			uppercaseCount = 1;
		}
		//if mixed, downcase acronyms of more than two letters to match the C# standard
		else if (isMixed)
		{
			bool isTla = false;
			if (FChar::IsUpper(ch))
			{
				uppercaseCount++;
				if (uppercaseCount > 2)
				{
					//if we're at the end, it's easy, just check the count
					if (i + 1 >= len)
					{
						isTla = uppercaseCount >= 3;
					}
					else
					{
						auto next = SnakeCaseString[i + 1];
						// if the next letter is lowercase, this letter is not part of a TLA, it's the start of a new word
						isTla = !FChar::IsLower(next) &&
							//else if we have enought uppercase behind us, or it's uppercase, we have a TLA
							(uppercaseCount >= 3 || FChar::IsUpper(next));
					}
				}

				if (isTla)
				{
					ch = FChar::ToLower(ch);
				}
			}
		}
		else
		{
			//by default, lowercase the rest
			bool makeLower = hasUpper;
			bool makeUpper = false;

			// C# standard is for acronyms of two letters to be capitalized, while TLAs become Tlas.
			// We cannot determine this automatically as we cannot semantically distinguish from two letter words.
			// However, for the case where the name starts with two letters followed by a digit, e.g. PixelFormat.BC4,
			// it seems reasonable to assume it's an acronym and not a word.
			if (i == 1 && i + 1 < len)
			{
				auto next = SnakeCaseString[i+1];
				if (uppercaseFirst && FChar::IsDigit(next))
				{
					makeLower = false;
					makeUpper = hasLower;
				}
			}

			if (makeLower)
			{
				ch = FChar::ToLower(ch);
			}
			else if (makeUpper)
			{
				ch = FChar::ToUpper(ch);
			}
		}

		PascalCaseString += ch;
	}

	return PascalCaseString;
}

FString FCSNameMapper::ScriptifyName(const FString& InName, const EScriptNameKind InNameKind) const
{
	FString MappedName = FScriptNameMapper::ScriptifyName(InName, InNameKind);

	switch (InNameKind)
	{
		case Property:
			// By default, leave the name as-is, PascalCasing and all.
			break;
		case Parameter:
			// By default, assume we're just converting PascalCase to camelCase.
			MappedName = FString::Chr(FChar::ToLower(InName[0])) + InName.RightChop(1);
			break;
		case Function:
			//we used to lop off "K2_" and "_NEW" but that shouldn't be necessary any more with ScriptName
			break;
		case EnumValue:
			MappedName = DeSnakifyName(InName, true);
		default:
			break;
	}

	return EscapeKeywords(MappedName);
}