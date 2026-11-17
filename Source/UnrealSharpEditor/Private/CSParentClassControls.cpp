#include "CSParentClassControls.h"

#include "CSSourceCodeNavigation.h"
#include "Engine/Blueprint.h"
#include "SourceCodeNavigation.h"
#include "Styling/AppStyle.h"
#include "Widgets/Input/SButton.h"
#include "Widgets/Input/SHyperlink.h"
#include "Widgets/Images/SImage.h"
#include "Widgets/SBoxPanel.h"
#include "Widgets/Text/STextBlock.h"

#define LOCTEXT_NAMESPACE "CSParentClassControls"

TSharedRef<SWidget> MakeCSParentClassControls(UBlueprint* Blueprint)
{
	const TWeakObjectPtr<UBlueprint> WeakBlueprint(Blueprint);
	const auto OpenSource = [WeakBlueprint]()
	{
		if (UBlueprint* Current = WeakBlueprint.Get())
		{
			FCSSourceCodeNavigation::OpenClass(Current->ParentClass);
		}
	};
	const FText ToolTip = FText::Format(LOCTEXT("OpenSource", "Open the C# parent class in {0}"),
		FSourceCodeNavigation::GetSelectedSourceCodeIDE());

	return SNew(SHorizontalBox)
		+ SHorizontalBox::Slot().AutoWidth().VAlign(VAlign_Center)
		[
			SNew(STextBlock)
			.ColorAndOpacity(FSlateColor::UseSubduedForeground())
			.Text(LOCTEXT("ParentClass", "Parent class: "))
		]
		+ SHorizontalBox::Slot().AutoWidth().VAlign(VAlign_Center).Padding(2.0f, 0.0f)
		[
			SNew(SHyperlink)
			.Style(FAppStyle::Get(), "Common.GotoNativeCodeHyperlink")
			.Text_Lambda([WeakBlueprint]()
			{
				UBlueprint* Current = WeakBlueprint.Get();
				return Current && Current->ParentClass ? Current->ParentClass->GetDisplayNameText() : FText::GetEmpty();
			})
			.ToolTipText(ToolTip)
			.OnNavigate_Lambda(OpenSource)
		]
		+ SHorizontalBox::Slot().AutoWidth().Padding(0.0f, 0.0f, 8.0f, 0.0f)
		[
			SNew(SButton)
			.ButtonStyle(FAppStyle::Get(), "HoverHintOnly")
			.ContentPadding(4.0f)
			.ToolTipText(ToolTip)
			.OnClicked_Lambda([OpenSource]() { OpenSource(); return FReply::Handled(); })
			[
				SNew(SImage).Image(FAppStyle::GetBrush("Icons.Edit"))
			]
		];
}

#undef LOCTEXT_NAMESPACE
