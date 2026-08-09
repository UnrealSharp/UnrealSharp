#pragma once

#include "CoreMinimal.h"
#include "Widgets/SCompoundWidget.h"
#include "Widgets/Input/SComboBox.h"

class IPlugin;

enum class ECSProjectDestinationMode : uint8
{
	Owners,
	Projects
};

struct FCSProjectDestination
{
	FCSProjectDestination(const FName InKey, FText InDisplayName, FString InName, FString InPath, const int32 InIndex, TSharedPtr<IPlugin> InPlugin = nullptr) :
		Key(InKey), DisplayName(MoveTemp(InDisplayName)), Name(MoveTemp(InName)), Path(MoveTemp(InPath)), Index(InIndex), Plugin(MoveTemp(InPlugin)) {}

	FName GetKey() const { return Key; }
	const FText& GetDisplayName() const { return DisplayName; }
	const FString& GetName() const { return Name; }
	const FString& GetPath() const { return Path; }
	int32 GetIndex() const { return Index; }
	const TSharedPtr<IPlugin>& GetPlugin() const { return Plugin; }

	FString GetRootDirectory() const;

private:
	FName Key;
	FText DisplayName;
	FString Name;
	FString Path;
	int32 Index;
	TSharedPtr<IPlugin> Plugin;

	friend uint32 GetTypeHash(const FCSProjectDestination& ProjectDestination)
	{
		return GetTypeHash(ProjectDestination.Key);
	}
};

class SCSProjectDestinationPicker : public SCompoundWidget
{
public:
	DECLARE_DELEGATE_TwoParams(FOnDestinationChanged, TSharedPtr<FCSProjectDestination>, ESelectInfo::Type);

	SLATE_BEGIN_ARGS(SCSProjectDestinationPicker)
			: _Mode(ECSProjectDestinationMode::Owners)
		{}
		SLATE_ARGUMENT(ECSProjectDestinationMode, Mode)
		SLATE_EVENT(FOnDestinationChanged, OnDestinationChanged)
	SLATE_END_ARGS()

	void Construct(const FArguments& InArgs);

	const TArray<TSharedRef<FCSProjectDestination>>& GetDestinations() const { return Destinations; }
	TSharedPtr<FCSProjectDestination> GetSelectedDestination() const { return SelectedDestination; }
	bool HasDestinations() const { return !Destinations.IsEmpty(); }

	void SetSelectedDestination(const TSharedPtr<FCSProjectDestination>& InDestination);
	bool SelectDestinationForPath(const FString& InPath);

private:
	void OnSelectionChanged(TSharedPtr<FCSProjectDestination> NewDestination, ESelectInfo::Type SelectInfo);
	static TSharedRef<SWidget> OnGenerateDestinationWidget(TSharedRef<FCSProjectDestination> Destination);
	FText GetSelectedDisplayName() const;

	TSharedPtr<SComboBox<TSharedRef<FCSProjectDestination>>> DestinationComboBox;
	TArray<TSharedRef<FCSProjectDestination>> Destinations;
	TSharedPtr<FCSProjectDestination> SelectedDestination;

	ECSProjectDestinationMode Mode = ECSProjectDestinationMode::Owners;
	FOnDestinationChanged OnDestinationChanged;
	bool bSuppressNotification = false;
};
