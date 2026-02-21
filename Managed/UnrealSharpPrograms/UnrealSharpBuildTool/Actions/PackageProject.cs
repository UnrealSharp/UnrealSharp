using System.Collections.ObjectModel;

namespace UnrealSharpBuildTool.Actions;

public class PackageProject : BuildToolAction
{
    public override bool RunAction()
    {
        string archiveDirectoryPath = Program.GetArgument("ArchiveDirectory");
        
        if (string.IsNullOrEmpty(archiveDirectoryPath))
        {
            throw new Exception("ArchiveDirectory argument is required for the Publish action.");
        }
        
        Program.CopyGlobalJson();

        string rootProjectPath = Path.Combine(archiveDirectoryPath, Program.BuildToolOptions.ProjectName);
        string publishFolder = Program.GetOutputPath(rootProjectPath);
        string bindingsPath = Path.Combine(Program.BuildToolOptions.PluginDirectory, "Managed", "UnrealSharp");
        string packageOutputFolder = Path.Combine(Program.BuildToolOptions.PluginDirectory, "Intermediate", "Build", "Managed");

        string UETargetType = Program.GetArgument("UETargetType");
        
        if (string.IsNullOrEmpty(UETargetType))
        {
            throw new Exception("UETargetType argument is required for the Publish action.");
        }
        
        Collection<string> extraArguments =
        [
            "--self-contained",
            
            "--runtime",
            "win-x64",
            
			"-p:DisableWithEditor=true",
            "-p:GenerateDocumentationFile=false",
            
            $"-p:UETargetType={UETargetType}",
            
            $"-p:PublishDir=\"{publishFolder}\"",
            $"-p:OutputPath=\"{packageOutputFolder}\"",
        ];

        BuildSolution buildBindings = new BuildSolution(bindingsPath, extraArguments, BuildConfig.Publish);
        buildBindings.RunAction();
        
        BuildSolution buildUserSolution = new BuildSolution(Program.GetScriptFolder(), extraArguments, BuildConfig.Publish);
        buildUserSolution.RunAction();

        BuildEmitLoadOrder.EmitLoadOrder(packageOutputFolder, publishFolder);
        return true;
    }
}
