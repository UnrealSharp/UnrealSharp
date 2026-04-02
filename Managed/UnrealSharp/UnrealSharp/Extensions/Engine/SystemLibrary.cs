using UnrealSharp.Core;
using UnrealSharp.CoreUObject;
using UnrealSharp.Interop;

namespace UnrealSharp.Engine;

public partial class SystemLibrary
{
    /// <summary>
    /// Set a timer to call the specified action after the specified duration.
    /// </summary>
    /// <param name="action"> The function to call. </param>
    /// <param name="time"> The time in seconds before the function is called. </param>
    /// <param name="bLooping"> Whether the timer should loop. </param>
    /// <param name="initialStartDelay"> The initial delay before the timer starts. </param>
    /// <param name="initialStartDelayVariance"> The variance in the initial delay. </param>
    /// <exception cref="ArgumentException"> Thrown if the target of the action is not an UObject. </exception>
    public static FTimerHandle SetTimer(Action action, float time, bool bLooping, float initialStartDelay = 0.000000f)
    {
        unsafe
        {
            if (action.Target is not UnrealSharpObject owner)
            {
                throw new ArgumentException("The target of the action must be an UObject.");
            }
            
            FTimerHandle timerHandle = new FTimerHandle();
            UWorldExporter.CallSetTimer(owner.NativeObject, action.Method.Name, time, bLooping.ToNativeBool(), initialStartDelay, &timerHandle);
            return timerHandle;
        }
    }
    
    /// <summary>
    /// Does a collision trace along the given line and returns the first blocking hit encountered.
    /// This trace finds the objects that RESPONDS to the given TraceChannel
    /// </summary>
    public static bool LineTraceByChannel(FVector start, FVector end, ETraceTypeQuery traceChannel, bool traceComplex, IList<AActor> actorsToIgnore, out FHitResult outHit)
    {
        return LineTraceByChannel(start, end, traceChannel, traceComplex, actorsToIgnore, EDrawDebugTrace.None, out outHit, true);
    }
    
    public static bool LineTraceByChannel(FVector start, FVector end, ETraceTypeQuery traceChannel, bool traceComplex, out FHitResult outHit)
    {
        return LineTraceByChannel(start, end, traceChannel, traceComplex, new List<AActor>(), EDrawDebugTrace.None, out outHit, true);
    }
    
    public static bool MultiLineTraceByChannel(FVector start, FVector end, ETraceTypeQuery traceChannel, bool traceComplex, out IList<FHitResult> outHits)
    {
        return MultiLineTraceByChannel(start, end, traceChannel, traceComplex, new List<AActor>(), EDrawDebugTrace.None, out outHits, true);
    }
    
    public static bool SphereTraceByChannel(FVector start, FVector end, float radius, ETraceTypeQuery traceChannel, bool traceComplex, out FHitResult outHit)
    {
        return SphereTraceByChannel(start, end, radius, traceChannel, traceComplex, new List<AActor>(), EDrawDebugTrace.None, out outHit, true);
    }
    
    public static bool MultiSphereTraceByChannel(FVector start, FVector end, float radius, ETraceTypeQuery traceChannel, bool traceComplex, out IList<FHitResult> outHits)
    {
        return MultiSphereTraceByChannel(start, end, radius, traceChannel, traceComplex, new List<AActor>(), EDrawDebugTrace.None, out outHits, true);
    }
    
    public static bool BoxTraceByChannel(FVector start, FVector end, FVector halfSize, FRotator orientation, ETraceTypeQuery traceChannel, bool traceComplex, out FHitResult outHit)
    {
        return BoxTraceByChannel(start, end, halfSize, orientation, traceChannel, traceComplex, new List<AActor>(), EDrawDebugTrace.None, out outHit, true);
    }
    
    public static bool MultiBoxTraceByChannel(FVector start, FVector end, FVector halfSize, FRotator orientation, ETraceTypeQuery traceChannel, bool traceComplex, out IList<FHitResult> outHits)
    {
        return MultiBoxTraceByChannel(start, end, halfSize, orientation, traceChannel, traceComplex, new List<AActor>(), EDrawDebugTrace.None, out outHits, true);
    }
}