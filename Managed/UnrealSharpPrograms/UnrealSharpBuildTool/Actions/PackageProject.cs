namespace UnrealSharpBuildTool.Actions;

public class PackageProject : BuildToolAction
{
    private static void CopyAssemblies(string destinationPath, string sourcePath)
    {
        if (!Directory.Exists(destinationPath)) 
        {
            Directory.CreateDirectory(destinationPath);
        }

        try
        {
            string[] dependencies = Directory.GetFiles(sourcePath, "*.*");
            foreach (var dependency in dependencies) 
            {
                var destPath = Path.Combine(destinationPath, Path.GetFileName(dependency));
                if (!File.Exists(destPath) || new FileInfo(dependency).LastWriteTimeUtc > new FileInfo(destPath).LastWriteTimeUtc)
                {
                    File.Copy(dependency, destPath, true);
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to copy dependencies from {sourcePath}: {ex.Message}");
        }
    }
    
    public override bool RunAction()
    {
        BuildSolution buildSolution = new BuildSolution();
        buildSolution.RunAction();
        
        WeaveProject weaveProject = new WeaveProject();
        weaveProject.RunAction();
        
        CopyAssemblies(Program.GetOutputPath(), Program.GetBindingsPath());

        return true;
    }
}