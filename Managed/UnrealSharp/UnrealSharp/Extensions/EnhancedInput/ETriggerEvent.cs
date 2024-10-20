using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnrealSharp.Attributes;

namespace UnrealSharp.EnhancedInput;


[UEnum, Flags]
public enum ETriggerEvent : byte
{   
    // No significant trigger state changes occurred and there are no active device inputs
    None = (0x0),
    // Triggering occurred after one or more processing ticks
    Triggered = (1 << 0),   // ETriggerState (None -> Triggered, Ongoing -> Triggered, Triggered -> Triggered)

    // An event has occurred that has begun Trigger evaluation. Note: Triggered may also occur this frame, but this event will always be fired first.
    Started = (1 << 1), // ETriggerState (None -> Ongoing, None -> Triggered)

    // Triggering is still being processed. For example, an action with a "Press and Hold" trigger
    // will be "Ongoing" while the user is holding down the key but the time threshold has not been met yet. 
    Ongoing = (1 << 2), // ETriggerState (Ongoing -> Ongoing)

    // Triggering has been canceled. For example,  the user has let go of a key before the "Press and Hold" time threshold.
    // The action has started to be evaluated, but never completed. 
    Canceled = (1 << 3),    // ETriggerState (Ongoing -> None)

    // The trigger state has transitioned from Triggered to None this frame, i.e. Triggering has finished.
    // Note: Using this event restricts you to one set of triggers for Started/Completed events. You may prefer two actions, each with its own trigger rules.
    // Completed will not fire if any trigger reports Ongoing on the same frame, but both should fire. e.g. Tick 2 of Hold (= Ongoing) + Pressed (= None) combo will raise Ongoing event only.
    Completed = (1 << 4),	// ETriggerState (Triggered -> None)
}
