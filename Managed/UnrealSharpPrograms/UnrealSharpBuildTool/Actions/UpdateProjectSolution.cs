using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace UnrealSharpBuildTool.Actions;

public partial class UpdateProjectSolution : BuildToolAction
{
    private ImmutableList<string> _pluginProjects = ImmutableList<string>.Empty;

    public override bool RunAction()
    {
        _pluginProjects = Program.GetArguments("PluginProject")
                .Select(x => GenerateProject.GetRelativePath(Program.GetScriptFolder(), x))
                .ToImmutableList();
        var exisingProjects = GetExistingProjects().ToHashSet();
        GenerateProject.AddProjectToSln(_pluginProjects.Except(exisingProjects).ToList());
        return true;
    }

    private IEnumerable<string> GetExistingProjects()
    {
        using var listExistingProjects = new BuildToolProcess();
        listExistingProjects.StartInfo.ArgumentList.Add("sln");
        listExistingProjects.StartInfo.ArgumentList.Add("list");

        listExistingProjects.StartInfo.WorkingDirectory = Program.GetScriptFolder();
        listExistingProjects.StartBuildToolProcess();

        return LineEndingRegex().Split(listExistingProjects.Output)
                .Select(x => x.Trim())
                .Where(x => x.EndsWith(".csproj"));
    }

    [GeneratedRegex("\r\n|\r|\n")]
    private static partial Regex LineEndingRegex();
}
