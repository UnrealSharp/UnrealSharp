using System;
using System.IO;
using System.Linq;
using AutomationTool;
using EpicGames.Core;
using UnrealBuildTool;
using UnrealSharp.Automation.BuildCommands;
using UnrealSharp.Automation.Utilities;

namespace UnrealSharp.Automation.Staging;

public class UnrealSharpStagingHandler : CustomStagingHandler
{
    protected override bool TryInitialize(ProjectParams Params, DeploymentContext SC)
    {
        // Automation modules can be discovered even when their plugin is disabled.
        // The native receipt identifies the plugins actually used by this target.
        return SC.StageTargets.Any(target => UsesUnrealSharp(target.Receipt));
    }

    public override void GetFilesToStage(ProjectParams Params, DeploymentContext SC)
    {
        TargetReceipt[] Receipts = SC.StageTargets
            .Select(target => target.Receipt)
            .Where(UsesUnrealSharp)
            .ToArray();
        TargetReceipt Receipt = Receipts[0];

        // All managed assemblies share one runtime directory. Different target
        // configurations or architectures cannot safely share that directory.
        if (Receipts.Length != 1 || SC.StageTargets.Any(target =>
                target.Receipt.TargetType is TargetType.Game or TargetType.Client or TargetType.Server &&
                target.Receipt != Receipt))
        {
            throw new AutomationException("UnrealSharp requires a single game, client or server target per staging directory. Stage different targets and configurations separately.");
        }
        if (Receipt.Platform != UnrealTargetPlatform.Win64 && Receipt.Platform != UnrealTargetPlatform.Mac)
        {
            throw new AutomationException($"UnrealSharp managed staging does not support platform '{Receipt.Platform}'.");
        }
        if (Receipt.Architectures.Architectures.Count != 1)
        {
            throw new AutomationException("UnrealSharp managed staging requires a single architecture. Universal Mac packages are not supported.");
        }
        if (Receipt.ProjectFile != null && Receipt.ProjectFile != SC.RawProjectPath)
        {
            throw new AutomationException($"The UnrealSharp target receipt belongs to a different project: {Receipt.ProjectFile}");
        }

        PackageProject Command = new()
        {
            Params =
            [
                $"Project={SC.RawProjectPath.FullName}",
                $"TargetPlatform={Receipt.Platform}",
                $"UETargetType={Receipt.TargetType}",
                $"UEBuildConfig={Receipt.Configuration}"
            ]
        };
        FileReference PluginFile = Receipt.RuntimeDependencies
            .Single(dependency => dependency.Path.GetFileName().Equals("UnrealSharp.uplugin", StringComparison.OrdinalIgnoreCase)).Path;
        if (Command.GetUnrealSharpUPlugin() != PluginFile)
        {
            throw new AutomationException($"The UnrealSharp plugin selected for publishing does not match the native target receipt: {PluginFile}");
        }

        string PublishRoot = Path.Combine(Command.GetUnrealSharpIntermediateDirectory(), "Publish",
            Receipt.TargetName, Receipt.Platform.ToString(), Receipt.Configuration.ToString(), Receipt.Architectures.SingleArchitecture.ToString());
        Directory.CreateDirectory(PublishRoot);

        LoggerUtilities.LogUnrealSharpInfo($"Publishing managed code for staged target '{Receipt.TargetName}'.");
        Command.StartPackaging(new PackageProject.PackagingOptions(PublishRoot, Receipt.TargetType,
            Receipt.Configuration, Receipt.Platform, Receipt.Architectures.SingleArchitecture, NativeAot: false));

        // Keep these files loose for hostfxr and the managed assembly loader.
        // UAT applies the platform's normal remapping, packaging and signing afterwards.
        DirectoryReference PublishDirectory = new(PathUtilities.BuildOutputPath(PublishRoot));
        StagedDirectoryReference StageDirectory = StagedDirectoryReference.Combine(SC.RelativeProjectRootForStage,
            "Binaries", "Managed", PublishDirectory.GetDirectoryName());
        SC.StageFiles(StagedFileType.NonUFS, PublishDirectory, "*", StageFilesSearch.AllDirectories, StageDirectory);
    }

    public override void GetFilesToStageForDLC(ProjectParams Params, DeploymentContext SC)
    {
        throw new AutomationException("UnrealSharp managed staging does not support DLC. Publish the managed code as part of the base game.");
    }

    private static bool UsesUnrealSharp(TargetReceipt receipt)
    {
        return receipt.TargetType is TargetType.Game or TargetType.Client or TargetType.Server &&
            receipt.RuntimeDependencies.Any(dependency =>
                dependency.Path.GetFileName().Equals("UnrealSharp.uplugin", StringComparison.OrdinalIgnoreCase));
    }
}
