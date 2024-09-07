namespace UnrealSharpBuildTool.Actions;

public class GenerateSolution : BuildToolAction
{
    public override bool RunAction()
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
        
        GenerateProject.AddProjectToSln(existingProjectsList);
        return true;
    }
}