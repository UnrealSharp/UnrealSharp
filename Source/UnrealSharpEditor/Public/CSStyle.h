#pragma once

namespace UnrealSharp::Icons
{
	UNREALSHARPEDITOR_API FSlateIcon GetUnrealSharpIcon();
	UNREALSHARPEDITOR_API FSlateIcon GetUnrealSharpIcon_HotReloadFailed();
	UNREALSHARPEDITOR_API FSlateIcon GetUnrealSharpIcon_Modified();
}

class FCSStyle
{
public:

	static void Initialize();
	static void Shutdown();

	static void ReloadTextures();
	
	static const ISlateStyle& Get();

	static FName GetStyleSetName();

private:

	static TSharedRef<class FSlateStyleSet> Create();
	static TSharedPtr<FSlateStyleSet> StyleInstance;
};
