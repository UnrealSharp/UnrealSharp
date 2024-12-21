using System.Collections.Concurrent;
using System.Runtime.InteropServices;
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
        public static Task ConfigureWithUnrealContext(this Task task, NamedThread thread = NamedThread.GameThread, bool thrownOnCancel = false)
        {
            var previousContext = SynchronizationContext.Current;
            var unrealContext = new UnrealSynchronizationContext(thread);

            SynchronizationContext.SetSynchronizationContext(unrealContext);

            return task.ContinueWith((t) =>
            {
                SynchronizationContext.SetSynchronizationContext(previousContext);

                if (thrownOnCancel && t.IsCanceled)
                {
                    throw new TaskCanceledException();
                }
            });
        }

        public static Task ConfigureWithUnrealContext(this ValueTask task, NamedThread thread = NamedThread.GameThread, bool thrownOnCancel = false)
        {
            var previousContext = SynchronizationContext.Current;
            var unrealContext = new UnrealSynchronizationContext(thread);

            SynchronizationContext.SetSynchronizationContext(unrealContext);

            return task.AsTask().ContinueWith((t) =>
            {
                SynchronizationContext.SetSynchronizationContext(previousContext);

                if (thrownOnCancel && t.IsCanceled)
                {
                    throw new TaskCanceledException();
                }
            });
        }


        public static Task<T> ConfigureWithUnrealContext<T>(this Task<T> task, NamedThread thread = NamedThread.GameThread, bool thrownOnCancel = false)
        {
            var previousContext = SynchronizationContext.Current;
            var unrealContext = new UnrealSynchronizationContext(thread);

            SynchronizationContext.SetSynchronizationContext(unrealContext);

            return task.ContinueWith((t) =>
            {
                SynchronizationContext.SetSynchronizationContext(previousContext);

                if (thrownOnCancel && t.IsCanceled)
                {
                    throw new TaskCanceledException();
                }

                return t.Result;
            });
        }

        public static Task<T> ConfigureWithUnrealContext<T>(this ValueTask<T> task, NamedThread thread = NamedThread.GameThread, bool thrownOnCancel = false)
        {
            var previousContext = SynchronizationContext.Current;
            var unrealContext = new UnrealSynchronizationContext(thread);

            SynchronizationContext.SetSynchronizationContext(unrealContext);

            return task.AsTask().ContinueWith((t) =>
            {
                SynchronizationContext.SetSynchronizationContext(previousContext);

                if (thrownOnCancel && t.IsCanceled)
                {
                    throw new TaskCanceledException();
                }

                return t.Result;
            });
        }
    }

    public class UnrealSynchronizationContext : SynchronizationContext
    {
        public static NamedThread CurrentThread => (NamedThread)AsyncExporter.CallGetCurrentNamedThread();

        private NamedThread thread;
        private nint worldContextObject;

        public UnrealSynchronizationContext(NamedThread thread)
        {
            this.thread = thread;
            worldContextObject = FCSManagerExporter.CallGetCurrentWorldContext();
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            RunOnThread(worldContextObject, thread, () => d(state));
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            if (CurrentThread == thread)
            {
                d(state);
                return;
            }
            var semaphore = new ManualResetEventSlim(initialState: false);
            RunOnThread(worldContextObject, thread, () =>
            {
                d(state);
                semaphore.Set();
            });
            semaphore.Wait();
        }

        internal static void RunOnThread(nint worldContextObject, NamedThread thread, Action callback)
        {
            unsafe
            {
                GCHandle gcHandle = GCHandle.Alloc(callback);
                AsyncExporter.CallRunOnThread(worldContextObject, (int)thread, GCHandle.ToIntPtr(gcHandle));
            }
        }

        public static void RunOnThread(UObject worldContextObject, NamedThread thread, Action callback)
        {
            unsafe
            {
                GCHandle gcHandle = GCHandle.Alloc(callback);
                AsyncExporter.CallRunOnThread(worldContextObject.NativeObject, (int)thread, GCHandle.ToIntPtr(gcHandle));
            }
        }
    }
}
