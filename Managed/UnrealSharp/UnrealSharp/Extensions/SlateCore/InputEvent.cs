using UnrealSharp.CoreUObject;
using UnrealSharp.UnrealSharpCore;

namespace UnrealSharp.SlateCore;

public partial struct FInputEvent
{
    public bool IsRepeat => UCSInputEventExtensions.IsRepeat(this);

    public bool IsShiftDown => UCSInputEventExtensions.IsShiftDown(this);
    public bool IsLeftShiftDown => UCSInputEventExtensions.IsLeftShiftDown(this);
    public bool IsRightShiftDown => UCSInputEventExtensions.IsRightShiftDown(this);

    public bool IsControlDown => UCSInputEventExtensions.IsControlDown(this);
    public bool IsLeftControlDown => UCSInputEventExtensions.IsLeftControlDown(this);
    public bool IsRightControlDown => UCSInputEventExtensions.IsRightControlDown(this);

    public bool IsAltDown => UCSInputEventExtensions.IsAltDown(this);
    public bool IsLeftAltDown => UCSInputEventExtensions.IsLeftAltDown(this);
    public bool IsRightAltDown => UCSInputEventExtensions.IsRightAltDown(this);

    public bool IsCommandDown => UCSInputEventExtensions.IsCommandDown(this);
    public bool IsLeftCommandDown => UCSInputEventExtensions.IsLeftCommandDown(this);
    public bool IsRightCommandDown => UCSInputEventExtensions.IsRightCommandDown(this);

    public bool AreCapsLocked => UCSInputEventExtensions.AreCapsLocked(this);

    public uint UserIndex => UCSInputEventExtensions.GetUserIndex(this);

    public ulong EventTimestamp => UCSInputEventExtensions.GetEventTimestamp(this);
    public double MillisecondsSinceEvent => UCSInputEventExtensions.GetMillisecondsSinceEvent(this);
}
