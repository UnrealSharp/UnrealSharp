using System.Runtime.InteropServices;
using UnrealSharp.Core;
using UnrealSharp.CoreUObject;
using UnrealSharp.Interop;

namespace UnrealSharp
{
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

    public static class UnrealContextTaskExtension
    {
        public static Task ConfigureWithUnrealContext(this Task task, NamedThread thread = NamedThread.GameThread, bool throwOnCancel = false)
        {
            SynchronizationContext? previousContext = SynchronizationContext.Current;
            UnrealSynchronizationContext unrealContext = new UnrealSynchronizationContext(thread, task);
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

        public static ValueTask ConfigureWithUnrealContext(this ValueTask task, NamedThread thread = NamedThread.GameThread,
                                                           bool throwOnCancel = false)
        {
            return task.IsCompletedSuccessfully ? task : new ValueTask(task.AsTask().ConfigureWithUnrealContext(thread, throwOnCancel));
        }

        public static Task<T> ConfigureWithUnrealContext<T>(this Task<T> task, NamedThread thread = NamedThread.GameThread, bool throwOnCancel = false)
        {
            SynchronizationContext? previousContext = SynchronizationContext.Current;
            UnrealSynchronizationContext unrealContext = new UnrealSynchronizationContext(thread, task);
            SynchronizationContext.SetSynchronizationContext(unrealContext);

            return task.ContinueWith(t =>
            {
                SynchronizationContext.SetSynchronizationContext(previousContext);

                if (throwOnCancel && t.IsCanceled)
                {
                    throw new TaskCanceledException();
                }

                return t.Result;
            });
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
        private IDisposable? _task;

        public UnrealSynchronizationContext(NamedThread thread, IDisposable task)
        {
            if (FCSManagerExporter.WorldContextObject is not UObject worldContext || !worldContext.IsValid)
            {
                throw new InvalidOperationException("World context object is not valid.");
            }
            
            _thread = thread;
            _worldContext = new TWeakObjectPtr<UObject>(worldContext.World);
            _task = task;
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
            if (worldContextObject.IsValid())
            {
                GCHandle callbackHandle = GCHandle.Alloc(callback);
                AsyncExporter.CallRunOnThread(worldContextObject.Data, (int) thread, GCHandle.ToIntPtr(callbackHandle));
            }
            
            _task!.Dispose();
            _task = null;
        }
    }
}
