using UnrealSharp.Interop;

namespace UnrealSharp.Engine;

public partial class USystemLibrary
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
}