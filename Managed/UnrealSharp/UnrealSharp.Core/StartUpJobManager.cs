using System.Collections.Concurrent;
using System.Reflection;

namespace UnrealSharp.Core;

public static class StartupJobManager
{
    private static readonly ConcurrentDictionary<Assembly, ConcurrentQueue<Action>> JobsByAssembly = new();

    public static void Register(Assembly assembly, Action job)
    {
        ConcurrentQueue<Action> queue = JobsByAssembly.GetOrAdd(assembly, static _ => new ConcurrentQueue<Action>());
        queue.Enqueue(job);
    }

    public static void RunForAssembly(Assembly assembly)
    {
        if (!JobsByAssembly.TryRemove(assembly, out ConcurrentQueue<Action>? queue))
        {
            return;
        }

        while (queue.TryDequeue(out Action? jobToRun))
        {
            try
            {
                jobToRun();
            }
            catch (Exception ex)
            {
                LogUnrealSharpCore.LogError($"Exception while running startup job for assembly {assembly.FullName}: {ex}");
            }
        }
    }

    public static bool HasJobs(Assembly assembly) => JobsByAssembly.ContainsKey(assembly);
}