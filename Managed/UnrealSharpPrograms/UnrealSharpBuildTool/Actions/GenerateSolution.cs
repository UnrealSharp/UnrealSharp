namespace UnrealSharpBuildTool.Actions;

public static class GenerateSolutionAction
{
    [Action("Generate Solution", "Generates a solution file for the current project.")]
    public static void GenerateSolution()
    {
        BuildToolProcess generateSln = new BuildToolProcess();
        
        // Create a solution.
        generateSln.StartInfo.ArgumentList.Add("new");
        generateSln.StartInfo.ArgumentList.Add("sln");
        
        // Assign project name to the solution.
        generateSln.StartInfo.ArgumentList.Add("-n");
        generateSln.StartInfo.ArgumentList.Add(Program.GetProjectNameAsManaged());
        generateSln.StartInfo.WorkingDirectory = Program.GetScriptFolder();
        
        // Force the creation of the solution.
        generateSln.StartInfo.ArgumentList.Add("--force");
        generateSln.StartBuildToolProcess();
        
        string[] existingProjects = Directory.GetFiles(Program.GetScriptFolder(), "*.csproj", SearchOption.AllDirectories);
        List<string> existingProjectsList = new List<string>(existingProjects.Length);
        
        foreach (string project in existingProjects)
        {
            string relativePath = Path.GetRelativePath(Program.GetScriptFolder(), project);
            existingProjectsList.Add(relativePath);
        }
        
        ProjectGeneration.GenerateProject.AddProjectToSln(existingProjectsList);
    }
}