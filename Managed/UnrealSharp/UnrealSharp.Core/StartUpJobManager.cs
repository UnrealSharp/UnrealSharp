namespace UnrealSharp.Core;

public static class StartUpJobManager
{
    private static readonly Dictionary<string, List<Action>> AssemblyStartupJobs = new();
    
    public static void RegisterStartUpJob(string assemblyName, Action initializer)
    {
        if (!AssemblyStartupJobs.TryGetValue(assemblyName, out List<Action>? value))
        {
            value = new List<Action>();
            AssemblyStartupJobs[assemblyName] = value;
        }

        value.Add(initializer);
    }
    
    public static void RunStartUpJobForAssembly(string assemblyName)
    {
        if (!AssemblyStartupJobs.TryGetValue(assemblyName, out List<Action>? initializers))
        {
            return;
        }
        
        foreach (Action initializer in initializers)
        {
            initializer();
        }
            
        AssemblyStartupJobs.Remove(assemblyName);
    }
    
    public static bool HasJobsForAssembly(string assemblyName) => AssemblyStartupJobs.ContainsKey(assemblyName);
}