#pragma once

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
