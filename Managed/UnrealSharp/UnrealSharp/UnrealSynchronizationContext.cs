using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using UnrealSharp.Core;
using UnrealSharp.CoreUObject;
using UnrealSharp.Interop;

namespace UnrealSharp;

public enum NamedThread
{
    UnusedAnchor = -1,

    /** The always-present, named threads are listed next **/
    RHIThread,
    GameThread,

    // The render thread is sometimes the game thread and is sometimes the actual rendering thread
    ActualRenderingThread = GameThread + 1,
    // CAUTION ThreadedRenderingThread must be the last named thread, insert new named threads before it

    /** not actually a thread index. Means "Unknown Thread" or "Any Unnamed Thread" **/
    AnyThread = 0xff,

    /** High bits are used for a queue index and priority**/

    MainQueue = 0x000,
    LocalQueue = 0x100,

    NumQueues = 2,
    ThreadIndexMask = 0xff,
    QueueIndexMask = 0x100,
    QueueIndexShift = 8,

    /** High bits are used for a queue index task priority and thread priority**/

    NormalTaskPriority = 0x000,
    HighTaskPriority = 0x200,

    NumTaskPriorities = 2,
    TaskPriorityMask = 0x200,
    TaskPriorityShift = 9,

    NormalThreadPriority = 0x000,
    HighThreadPriority = 0x400,
    BackgroundThreadPriority = 0x800,

    NumThreadPriorities = 3,
    ThreadPriorityMask = 0xC00,
    ThreadPriorityShift = 10,

    /** Combinations **/
    GameThread_Local = GameThread | LocalQueue,
    ActualRenderingThread_Local = ActualRenderingThread | LocalQueue,

    AnyHiPriThreadNormalTask = AnyThread | HighThreadPriority | NormalTaskPriority,
    AnyHiPriThreadHiPriTask = AnyThread | HighThreadPriority | HighTaskPriority,

    AnyNormalThreadNormalTask = AnyThread | NormalThreadPriority | NormalTaskPriority,
    AnyNormalThreadHiPriTask = AnyThread | NormalThreadPriority | HighTaskPriority,

    AnyBackgroundThreadNormalTask = AnyThread | BackgroundThreadPriority | NormalTaskPriority,
    AnyBackgroundHiPriTask = AnyThread | BackgroundThreadPriority | HighTaskPriority,
};

public sealed class TrackedTaskInfo
{
    public Task Task { get; }
    public NamedThread RequestedThread { get; }
    public DateTimeOffset StartTime { get; }
    public DateTimeOffset? EndTime { get; private set; }
    public TaskStatus? FinalStatus { get; private set; }
    public Exception? Exception { get; private set; }

    internal TrackedTaskInfo(Task task, NamedThread thread)
    {
        Task = task ?? throw new ArgumentNullException(nameof(task));
        RequestedThread = thread;
        StartTime = DateTimeOffset.UtcNow;
    }

    internal void MarkCompleted(Task task)
    {
        EndTime = DateTimeOffset.UtcNow;
        FinalStatus = task.Status;
        
        if (task.IsFaulted)
        {
            Exception = task.Exception;
        }
        
        task.Dispose();
    }

    public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;
}

public static class TaskTracker
{
    private static readonly ConcurrentDictionary<Task, TrackedTaskInfo> Tracked = new();
    
    public static int ActiveCount => Tracked.Count;
    
    public static event Action<TrackedTaskInfo>? TaskRegistered;
    public static event Action<TrackedTaskInfo>? TaskUnregistered;

    internal static void RegisterTask(Task task, NamedThread thread)
    {
        TrackedTaskInfo info = new TrackedTaskInfo(task, thread);
        
        Tracked.TryAdd(task, info);
        TaskRegistered?.Invoke(info);
    }

    internal static void UnregisterTask(Task? task)
    {
        if (task == null)
        {
            LogUnrealSharp.LogWarning("Attempted to unregister a null task.");
            return;
        }
        
        if (!Tracked.TryRemove(task, out var info))
        {
            LogUnrealSharp.LogWarning("Attempted to unregister a task that was not registered.");
            return;
        }
        
        info.MarkCompleted(task);
        TaskUnregistered?.Invoke(info);
    }
    
    public static IReadOnlyList<TrackedTaskInfo> GetActiveTasksSnapshot()
    {
        return Tracked.Values.ToArray();
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void WaitForAllActiveTasks()
    {
        Task[] tasks = Tracked.Keys.ToArray();
        int count = tasks.Length;
        
        if (count == 0)
        {
            return;
        }

        LogUnrealSharp.Log($"Waiting for {count} active tasks to complete...");
        
        foreach (TrackedTaskInfo trackedTask in Tracked.Values)
        {
            LogUnrealSharp.Log($" - Task {trackedTask.Task.Id} currently in state {trackedTask.Task.Status}, requested thread {trackedTask.RequestedThread}");
        }
        
        Task.WhenAll(tasks).GetAwaiter().GetResult();
        
        LogUnrealSharp.Log("All active tasks have completed.");
    }
}

public static class UnrealContextTaskExtension
{
    public static Task ConfigureWithUnrealContext(this Task task, NamedThread thread = NamedThread.GameThread, bool throwOnCancel = false)
    {
        SynchronizationContext? previousContext = SynchronizationContext.Current;
        UnrealSynchronizationContext unrealContext = new UnrealSynchronizationContext(thread);
        SynchronizationContext.SetSynchronizationContext(unrealContext);

        return task.ContinueWith(t =>
        {
            SynchronizationContext.SetSynchronizationContext(previousContext);

            if (throwOnCancel && t.IsCanceled)
            {
                throw new TaskCanceledException();
            }
        });
    }

    public static ValueTask ConfigureWithUnrealContext(this ValueTask task, NamedThread thread = NamedThread.GameThread, bool throwOnCancel = false)
    {
        return task.IsCompletedSuccessfully ? task : new ValueTask(task.AsTask().ConfigureWithUnrealContext(thread, throwOnCancel));
    }

    public static Task<T> ConfigureWithUnrealContext<T>(this Task<T> task, NamedThread thread = NamedThread.GameThread, bool throwOnCancel = false)
    {
        if (task == null)
        {
            throw new ArgumentNullException(nameof(task));
        }
        
        TaskTracker.RegisterTask(task, thread);

        SynchronizationContext? previousContext = SynchronizationContext.Current;
        UnrealSynchronizationContext unrealContext = new UnrealSynchronizationContext(thread);
        SynchronizationContext.SetSynchronizationContext(unrealContext);

        return task.ContinueWith(finishedTask =>
        {
            try
            {
                if (throwOnCancel && finishedTask.IsCanceled)
                {
                    throw new TaskCanceledException();
                }
                
                if (finishedTask.IsFaulted)
                {
                    ExceptionDispatchInfo.Capture(finishedTask.Exception!).Throw();
                }

                return finishedTask.Result;
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previousContext);
                TaskTracker.UnregisterTask(finishedTask);
            }
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);
    }

    public static ValueTask<T> ConfigureWithUnrealContext<T>(this ValueTask<T> task,
        NamedThread thread = NamedThread.GameThread,
        bool throwOnCancel = false)
    {
        return task.IsCompletedSuccessfully ? task : new ValueTask<T>(task.AsTask().ConfigureWithUnrealContext(thread, throwOnCancel));
    }
}
    
public sealed class UnrealSynchronizationContext : SynchronizationContext
{
    public static NamedThread CurrentThread => (NamedThread)AsyncExporter.CallGetCurrentNamedThread();
        
    private readonly NamedThread _thread;
    private readonly TWeakObjectPtr<UObject> _worldContext;

    public UnrealSynchronizationContext(NamedThread thread)
    {
        if (FCSManagerExporter.WorldContextObject is not UObject worldContext || !worldContext.IsValid())
        {
            throw new InvalidOperationException("World context object is not valid.");
        }
            
        _thread = thread;
        _worldContext = new TWeakObjectPtr<UObject>(worldContext.World);
    }

    public override void Post(SendOrPostCallback d, object? state) => RunOnThread(_worldContext, _thread, () => d(state));
    public override void Send(SendOrPostCallback d, object? state)
    {
        if (CurrentThread == _thread)
        {
            d(state);
            return;
        }

        using ManualResetEventSlim manualResetEventInstance = new ManualResetEventSlim(false);
            
        RunOnThread(_worldContext, _thread, () =>
        {
            try
            {
                d(state);
            }
            finally
            {
                manualResetEventInstance.Set();
            }
        });
        manualResetEventInstance.Wait();
    }

    void RunOnThread(TWeakObjectPtr<UObject> worldContextObject, NamedThread thread, Action callback)
    {
        if (!worldContextObject.IsValid)
        {
            return;
        }
        
        GCHandle callbackHandle = GCHandle.Alloc(callback);
        AsyncExporter.CallRunOnThread(worldContextObject.Data, (int) thread, GCHandle.ToIntPtr(callbackHandle));
    }
}