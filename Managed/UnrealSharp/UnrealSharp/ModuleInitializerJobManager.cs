using System.Collections.Concurrent;

namespace UnrealSharp;

using System.Reflection;

public enum ModuleInitializerJobType : byte
{
    TypeRegistration,
    ModuleInterfaceInit,
}

public readonly struct ModuleInitializerJob : IEquatable<ModuleInitializerJob>
{
    public Assembly ModuleAssembly { get; }
    public ModuleInitializerJobType JobType { get; }

    public ModuleInitializerJob(Assembly moduleAssembly, ModuleInitializerJobType jobType)
    {
        ModuleAssembly = moduleAssembly;
        JobType = jobType;
    }
    
    public bool Equals(ModuleInitializerJob other) => ReferenceEquals(ModuleAssembly, other.ModuleAssembly) && JobType == other.JobType;
    public override bool Equals(object? obj) => obj is ModuleInitializerJob other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(ModuleAssembly, JobType);
}

public static class ModuleInitializerJobManager
{
    private static readonly ConcurrentDictionary<ModuleInitializerJob, List<Action>> Jobs = new();

    public static void AddJob(Type calleeType, ModuleInitializerJobType jobType, Action action) => AddJob(calleeType.Assembly, jobType, action);
    public static void AddJob(Assembly moduleAssembly, ModuleInitializerJobType jobType, Action action)
    {
        ModuleInitializerJob job = new ModuleInitializerJob(moduleAssembly, jobType);
        List<Action> actions = Jobs.GetOrAdd(job, static _ => new List<Action>());
        
        lock (actions)
        {
            actions.Add(action);
        }
    }
    
    public static void ExecuteJobsForModule(Assembly moduleAssembly, ModuleInitializerJobType jobType)
    {
        ModuleInitializerJob job = new ModuleInitializerJob(moduleAssembly, jobType);
        
        if (!Jobs.TryRemove(job, out List<Action>? actions))
        {
            return;
        }

        foreach (Action action in actions)
        {
            action();
        }
    }
    
    public static void ExecuteAllJobsForModule(Assembly moduleAssembly)
    {
        foreach (ModuleInitializerJobType jobType in Enum.GetValues<ModuleInitializerJobType>())
        {
            ExecuteJobsForModule(moduleAssembly, jobType);
        }
    }
    
    public static void RemoveAllJobsForModule(Assembly moduleAssembly)
    {
        foreach (ModuleInitializerJob key in Jobs.Keys)
        {
            if (ReferenceEquals(key.ModuleAssembly, moduleAssembly))
            {
                Jobs.TryRemove(key, out _);
            }
        }
    }
}