using UnrealSharp.CoreUObject;
using UnrealSharp.InputCore;
using UnrealSharp.UnrealSharpCore;

namespace UnrealSharp.SlateCore;

public partial struct FKeyEvent
{
    public override string ToString()
    {
        FKey key = UCSKeyEventExtensions.GetKey(this);
        return !key.IsValid ? "Invalid Key" : key.ToString();
    }

    public FKey Key => UCSKeyEventExtensions.GetKey(this);
    public uint Character => UCSKeyEventExtensions.GetCharacter(this);
    public uint KeyCode => UCSKeyEventExtensions.GetKeyCode(this);
    public FText Text => UCSKeyEventExtensions.ToText(this);

    public bool IsKeyEvent => UCSKeyEventExtensions.IsKeyEvent(this);
    public bool IsRepeat => UCSKeyEventExtensions.IsRepeat(this);

    public bool IsShiftDown => UCSKeyEventExtensions.IsShiftDown(this);
    public bool IsLeftShiftDown => UCSKeyEventExtensions.IsLeftShiftDown(this);
    public bool IsRightShiftDown => UCSKeyEventExtensions.IsRightShiftDown(this);

    public bool IsControlDown => UCSKeyEventExtensions.IsControlDown(this);
    public bool IsLeftControlDown => UCSKeyEventExtensions.IsLeftControlDown(this);
    public bool IsRightControlDown => UCSKeyEventExtensions.IsRightControlDown(this);

    public bool IsAltDown => UCSKeyEventExtensions.IsAltDown(this);
    public bool IsLeftAltDown => UCSKeyEventExtensions.IsLeftAltDown(this);
    public bool IsRightAltDown => UCSKeyEventExtensions.IsRightAltDown(this);

    public bool IsCommandDown => UCSKeyEventExtensions.IsCommandDown(this);
    public bool IsLeftCommandDown => UCSKeyEventExtensions.IsLeftCommandDown(this);
    public bool IsRightCommandDown => UCSKeyEventExtensions.IsRightCommandDown(this);

    public bool AreCapsLocked => UCSKeyEventExtensions.AreCapsLocked(this);

    public uint UserIndex => UCSKeyEventExtensions.GetUserIndex(this);
}
