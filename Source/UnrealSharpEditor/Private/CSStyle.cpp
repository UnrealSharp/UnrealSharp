#include "CSStyle.h"

#include "Framework/Application/SlateApplication.h"
#include "Styling/SlateStyleRegistry.h"
#include "Interfaces/IPluginManager.h"
#include "Styling/SlateStyleMacros.h"

#define RootToContentDir Style->RootToContentDir

TSharedPtr<FSlateStyleSet> FCSStyle::StyleInstance = nullptr;

void FCSStyle::Initialize()
{
	if (!StyleInstance.IsValid())
	{
		StyleInstance = Create();
		FSlateStyleRegistry::RegisterSlateStyle(*StyleInstance);
	}
}

void FCSStyle::Shutdown()
{
	FSlateStyleRegistry::UnRegisterSlateStyle(*StyleInstance);
	ensure(StyleInstance.IsUnique());
	StyleInstance.Reset();
}

void FCSStyle::ReloadTextures()
{
	if (FSlateApplication::IsInitialized())
	{
		FSlateApplication::Get().GetRenderer()->ReloadTextureResources();
	}
}

const ISlateStyle& FCSStyle::Get()
{
	return *StyleInstance;
}

FName FCSStyle::GetStyleSetName()
{
	static FName StyleSetName(TEXT("UnrealSharpStyle"));
	return StyleSetName;
}

TSharedRef<FSlateStyleSet> FCSStyle::Create()
{
	const FVector2D Icon40x40(40.0f, 40.0f);
	
	TSharedRef<FSlateStyleSet> Style = MakeShareable(new FSlateStyleSet("UnrealSharpStyle"));
	Style->SetContentRoot(IPluginManager::Get().FindPlugin(UE_PLUGIN_NAME)->GetBaseDir() / TEXT("Resources"));
	Style->Set("UnrealSharp.Toolbar", new IMAGE_BRUSH(TEXT("Icon_Default_40x"), Icon40x40));
	Style->Set("UnrealSharp.Toolbar.Modified", new IMAGE_BRUSH(TEXT("Icon_Modified_40x"), Icon40x40));
	Style->Set("UnrealSharp.Toolbar.Fail", new IMAGE_BRUSH(TEXT("Icon_Fail_40x"), Icon40x40));
	return Style;
}
