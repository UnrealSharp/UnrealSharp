using System.Collections.Concurrent;
using System.Runtime.InteropServices;

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

    public class UnrealSynchronizationContext : SynchronizationContext
    {
        private static readonly ConcurrentDictionary<NamedThread, UnrealSynchronizationContext> syncContextCache = new();

        public static NamedThread CurrentThread => (NamedThread)AsyncExporter.CallGetCurrentNamedThread();

        public static UnrealSynchronizationContext GetContext(NamedThread thread)
            => syncContextCache.GetOrAdd(thread, static thread => new(thread));

        public NamedThread Thread { get; }

        private UnrealSynchronizationContext(NamedThread thread)
        {
            Thread = thread;
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            RunOnThread(Thread, () => d(state));
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            if (CurrentThread == Thread)
            {
                d(state);
                return;
            }
            var semaphore = new ManualResetEventSlim(initialState: false);
            RunOnThread(Thread, () =>
            {
                d(state);
                semaphore.Set();
            });
            semaphore.Wait();
        }

        public static void RunOnThread(NamedThread thread, Action callback)
        {
            unsafe
            {
                GCHandle gcHandle = GCHandle.Alloc(callback);
                AsyncExporter.CallRunOnThread((int)thread, GCHandle.ToIntPtr(gcHandle));
            }
        }

    }
}
