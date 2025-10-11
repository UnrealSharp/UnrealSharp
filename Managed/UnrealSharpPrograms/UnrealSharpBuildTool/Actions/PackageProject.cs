﻿using System.Collections.ObjectModel;

namespace UnrealSharpBuildTool.Actions;

public class PackageProject : BuildToolAction
{
    public override bool RunAction()
    {
        string archiveDirectoryPath = Program.TryGetArgument("ArchiveDirectory");
        
        if (string.IsNullOrEmpty(archiveDirectoryPath))
        {
            throw new Exception("ArchiveDirectory argument is required for the Publish action.");
        }

        string rootProjectPath = Path.Combine(archiveDirectoryPath, Program.BuildToolOptions.ProjectName);
        string binariesPath = Program.GetOutputPath(rootProjectPath);
        string bindingsPath = Path.Combine(Program.BuildToolOptions.PluginDirectory, "Managed", "UnrealSharp");
        string bindingsOutputPath = Path.Combine(Program.BuildToolOptions.PluginDirectory, "Intermediate", "Build", "Managed");
        
        Collection<string> extraArguments =
        [
            "--self-contained",
            "--runtime",
            "win-x64",
			"-p:DisableWithEditor=true",
            "-p:GenerateDocumentationFile=false",
            $"-p:PublishDir=\"{binariesPath}\"",
            $"-p:OutputPath=\"{bindingsOutputPath}\"",
        ];

        BuildSolution buildBindings = new BuildSolution(bindingsPath, extraArguments, BuildConfig.Publish);
        buildBindings.RunAction();
        
        BuildSolution buildUserSolution = new BuildSolution(Program.GetScriptFolder(), extraArguments, BuildConfig.Publish);
        buildUserSolution.RunAction();

        BuildEmitLoadOrder.EmitLoadOrder(binariesPath);

        return true;
    }
}
