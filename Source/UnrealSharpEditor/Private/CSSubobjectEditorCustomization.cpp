#include "CSSubobjectEditorCustomization.h"

#include "CSSourceCodeNavigation.h"
#include "SourceCodeNavigation.h"
#include "SubobjectData.h"
#include "Styling/AppStyle.h"
#include "Types/CSBlueprint.h"
#include "Types/CSClass.h"
#include "Widgets/Input/SHyperlink.h"

#define LOCTEXT_NAMESPACE "CSSubobjectEditorCustomization"

#if UE_VERSION_OLDER_THAN(5, 8, 0)
TSharedPtr<SWidget> FCSSubobjectEditorCustomization::GetControlsWidget(const FSubobjectData& Data) const
#else
TSharedPtr<SWidget> FCSSubobjectEditorCustomization::GetControlsWidget(TSharedRef<SSubobjectEditor>& SubobjectEditor, const FSubobjectData& Data) const
#endif
{
	const UClass* SourceClass = nullptr;
	if (!Data.IsNativeComponent())
	{
		if (const UCSBlueprint* Blueprint = Cast<UCSBlueprint>(Data.GetBlueprint()))
		{
			// An inherited component's declaration belongs to the C# actor that created it.
			SourceClass = Blueprint->GeneratedClass;
		}
	}

	if (!SourceClass)
	{
		if (const UObject* Object = Data.GetObject())
		{
			SourceClass = Cast<UCSClass>(Object->GetClass());
		}
	}

	if (!SourceClass)
	{
		// Leave ordinary Blueprint and native component controls to the engine.
		return nullptr;
	}

	TWeakObjectPtr<const UClass> WeakClass(SourceClass);
	return SNew(SHyperlink)
		.Style(FAppStyle::Get(), "Common.GotoNativeCodeHyperlink")
		.Text(LOCTEXT("EditInCSharp", "Edit in C#"))
		.ToolTipText(FText::Format(LOCTEXT("OpenSourceTooltip", "Open the C# declaration in {0}"), FSourceCodeNavigation::GetSelectedSourceCodeIDE()))
		.IsEnabled_Lambda([WeakClass]() { return WeakClass.IsValid(); })
		.OnNavigate_Lambda([WeakClass]()
		{
			FCSSourceCodeNavigation::OpenClass(WeakClass.Get());
		});
}

#undef LOCTEXT_NAMESPACE
