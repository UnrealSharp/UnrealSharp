using UnrealSharp.Core;
using UnrealSharp.UnrealSharpCore;

namespace UnrealSharp.InputCore;

public partial struct FKey
{
    public FKey(string keyName)
    {
        KeyName = new FName(keyName);
    }
    
    public override string ToString() => UCSKeyExtensions.ToString(this);

    public bool IsValid => UCSKeyExtensions.IsValid(this);
    public bool IsModifierKey => UCSKeyExtensions.IsModifierKey(this);
    public bool IsGamepadKey => UCSKeyExtensions.IsGamepadKey(this);
    public bool IsTouch => UCSKeyExtensions.IsTouch(this);
    public bool IsMouseButton => UCSKeyExtensions.IsMouseButton(this);
    public bool IsButtonAxis => UCSKeyExtensions.IsButtonAxis(this);
    public bool IsAxis1D => UCSKeyExtensions.IsAxis1D(this);
    public bool IsAxis2D => UCSKeyExtensions.IsAxis2D(this);
    public bool IsAxis3D => UCSKeyExtensions.IsAxis3D(this);
    public bool IsDigital => UCSKeyExtensions.IsDigital(this);
    public bool IsAnalog => UCSKeyExtensions.IsAnalog(this);
    public bool IsBindableInBlueprints => UCSKeyExtensions.IsBindableInBlueprints(this);
    public bool ShouldUpdateAxisWithoutSamples => UCSKeyExtensions.ShouldUpdateAxisWithoutSamples(this);
    public bool IsBindableToActions => UCSKeyExtensions.IsBindableToActions(this);
    public bool IsDeprecated => UCSKeyExtensions.IsDeprecated(this);
    public bool IsGesture => UCSKeyExtensions.IsGesture(this);

    public FText GetDisplayName(bool bLongDisplayName = true) => UCSKeyExtensions.GetDisplayName(this, bLongDisplayName);
    public FName GetMenuCategory() => UCSKeyExtensions.GetMenuCategory(this);
    public FKey GetPairedAxisKey() => UCSKeyExtensions.GetPairedAxisKey(this);
}
