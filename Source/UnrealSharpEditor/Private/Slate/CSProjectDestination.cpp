#include "Slate/CSProjectDestination.h"

#include "CSCommonGlobalSettings.h"
#include "CSPathsUtilities.h"
#include "HAL/FileManager.h"
#include "Interfaces/IPluginManager.h"
#include "Misc/App.h"
#include "Misc/Paths.h"
#include "Widgets/Text/STextBlock.h"

#define LOCTEXT_NAMESPACE "UnrealSharpEditor"

FString FCSProjectDestination::GetRootDirectory() const
{
	return Plugin.IsValid() ? Plugin->GetBaseDir() : FPaths::ProjectDir();
}

namespace UnrealSharp::ProjectDestinations
{
	static void GatherOwners(TArray<TSharedRef<FCSProjectDestination>>& OutDestinations)
	{
		static const FName ProjectDestinationKey(TEXT("<ProjectDestination>"));

		FString ScriptPath = FPaths::ConvertRelativePathToFull(Paths::GetScriptFolderDirectory());
		FPaths::NormalizeDirectoryName(ScriptPath);

		FText ProjectDisplayName = FText::FromString(FString::Printf(TEXT("%s (This Project)"), FApp::GetProjectName()));
		OutDestinations.Add(MakeShared<FCSProjectDestination>(ProjectDestinationKey, ProjectDisplayName, FApp::GetProjectName(), ScriptPath, OutDestinations.Num()));
		
		TArray<TSharedRef<IPlugin>> EnabledPlugins = IPluginManager::Get().GetEnabledPlugins();
		for (const TSharedRef<IPlugin>& Plugin : EnabledPlugins)
		{
			const FString PluginFilePath = FPaths::ConvertRelativePathToFull(Plugin->GetBaseDir());
			
			if (!FPaths::IsUnderDirectory(PluginFilePath, Paths::GetPluginsDirectory()) || Plugin->GetName() == UE_PLUGIN_NAME)
			{
				continue;
			}

			FString ScriptDirectory = PluginFilePath / GlobalSettings::Common::GetScriptDirectoryName();
			FPaths::NormalizeDirectoryName(ScriptDirectory);

			OutDestinations.Add(MakeShared<FCSProjectDestination>(FName(Plugin->GetName()), 
			                                                      FText::FromString(Plugin->GetFriendlyName()), 
			                                                      Plugin->GetName(), 
			                                                      ScriptDirectory, 
			                                                      OutDestinations.Num(), Plugin));
		}
	}

	static void GatherProjects(TArray<TSharedRef<FCSProjectDestination>>& OutDestinations)
	{
		TArray<TSharedRef<FCSProjectDestination>> Owners;
		GatherOwners(Owners);

		for (const TSharedRef<FCSProjectDestination>& Owner : Owners)
		{
			if (!FPaths::DirectoryExists(Owner->GetPath()))
			{
				continue;
			}

			TArray<FString> ProjectFiles;
			IFileManager::Get().FindFilesRecursive(ProjectFiles, *Owner->GetPath(), TEXT("*.csproj"), true, false);

			for (const FString& ProjectFile : ProjectFiles)
			{
				const FString FullProjectFile = FPaths::ConvertRelativePathToFull(ProjectFile);
				if (FullProjectFile.Contains(TEXT("/obj/")) || FullProjectFile.Contains(TEXT("/bin/")))
				{
					continue;
				}

				const FString ProjectName = FPaths::GetBaseFilename(FullProjectFile);

				FString ProjectDirectory = FPaths::GetPath(FullProjectFile);
				FPaths::NormalizeDirectoryName(ProjectDirectory);
				
				if (ProjectName.EndsWith(TEXT(".RuntimeGlue")))
				{
					continue;
				}

				FText DisplayName = FText::Format(LOCTEXT("ProjectDestinationDisplayName", "{0} ({1})"), FText::FromString(ProjectName), FText::FromString(Owner->GetName()));
				OutDestinations.Add(MakeShared<FCSProjectDestination>(*ProjectName, DisplayName, ProjectName, ProjectDirectory, OutDestinations.Num(), Owner->GetPlugin()));
			}
		}
	}

	void Gather(ECSProjectDestinationMode Mode, TArray<TSharedRef<FCSProjectDestination>>& OutDestinations)
	{
		OutDestinations.Reset();

		if (Mode == ECSProjectDestinationMode::Projects)
		{
			GatherProjects(OutDestinations);
		}
		else
		{
			GatherOwners(OutDestinations);
		}
	}

	TSharedPtr<FCSProjectDestination> FindForPath(const TArray<TSharedRef<FCSProjectDestination>>& Destinations, const FString& Path)
	{
		if (Path.IsEmpty())
		{
			return nullptr;
		}

		FString FullPath = FPaths::ConvertRelativePathToFull(Path);
		FPaths::NormalizeDirectoryName(FullPath);

		TSharedPtr<FCSProjectDestination> BestMatch;
		int32 BestMatchLength = INDEX_NONE;

		for (const TSharedRef<FCSProjectDestination>& Destination : Destinations)
		{
			FString DestinationPath = FPaths::ConvertRelativePathToFull(Destination->GetPath());
			FPaths::NormalizeDirectoryName(DestinationPath);

			if (!FPaths::IsUnderDirectory(FullPath, DestinationPath) || DestinationPath.Len() < BestMatchLength)
			{
				continue;
			}
			
			BestMatch = Destination;
			BestMatchLength = DestinationPath.Len();
		}

		return BestMatch;
	}
}

void SCSProjectDestinationPicker::Construct(const FArguments& InArgs)
{
	Mode = InArgs._Mode;
	OnDestinationChanged = InArgs._OnDestinationChanged;

	UnrealSharp::ProjectDestinations::Gather(Mode, Destinations);

	if (!Destinations.IsEmpty())
	{
		SelectedDestination = Destinations[0];
	}

	ChildSlot
	[
		SAssignNew(DestinationComboBox, SComboBox<TSharedRef<FCSProjectDestination>>)
		.OptionsSource(&Destinations)
		.InitiallySelectedItem(SelectedDestination)
		.OnSelectionChanged(this, &SCSProjectDestinationPicker::OnSelectionChanged)
		.OnGenerateWidget_Static(&SCSProjectDestinationPicker::OnGenerateDestinationWidget)
		.Content()
		[
			SNew(STextBlock)
			.Text(this, &SCSProjectDestinationPicker::GetSelectedDisplayName)
		]
	];
}

void SCSProjectDestinationPicker::OnSelectionChanged(TSharedPtr<FCSProjectDestination> NewDestination, ESelectInfo::Type SelectInfo)
{
	SelectedDestination = NewDestination;

	if (!bSuppressNotification)
	{
		OnDestinationChanged.ExecuteIfBound(SelectedDestination, SelectInfo);
	}
}

void SCSProjectDestinationPicker::SetSelectedDestination(const TSharedPtr<FCSProjectDestination>& InDestination)
{
	if (!DestinationComboBox.IsValid())
	{
		SelectedDestination = InDestination;
		return;
	}

	if (InDestination.IsValid())
	{
		DestinationComboBox->SetSelectedItem(InDestination);
	}
	else
	{
		DestinationComboBox->ClearSelection();
		SelectedDestination = nullptr;
	}
}

bool SCSProjectDestinationPicker::SelectDestinationForPath(const FString& InPath)
{
	TSharedPtr<FCSProjectDestination> Match = UnrealSharp::ProjectDestinations::FindForPath(Destinations, InPath);

	if (Match == SelectedDestination)
	{
		return Match.IsValid();
	}
	
	SetSelectedDestination(Match);
	return Match.IsValid();
}

TSharedRef<SWidget> SCSProjectDestinationPicker::OnGenerateDestinationWidget(TSharedRef<FCSProjectDestination> Destination)
{
	return SNew(STextBlock).Text(Destination->GetDisplayName());
}

FText SCSProjectDestinationPicker::GetSelectedDisplayName() const
{
	if (!SelectedDestination.IsValid())
	{
		return Destinations.IsEmpty() ? LOCTEXT("NoDestinationsAvailable", "No C# projects found") : LOCTEXT("NoDestinationSelected", "None");
	}

	return SelectedDestination->GetDisplayName();
}

#undef LOCTEXT_NAMESPACE
