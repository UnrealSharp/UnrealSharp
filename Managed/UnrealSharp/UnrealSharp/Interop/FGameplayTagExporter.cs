using UnrealSharp.GameplayTags;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public unsafe partial class FGameplayTagExporter
{
    public static delegate* unmanaged<ref Name, ref Name, NativeBool> MatchesTag;
    public static delegate* unmanaged<ref Name, ref Name, NativeBool> MatchesTagDepth;
    public static delegate* unmanaged<ref Name, ref GameplayTagContainer, NativeBool> MatchesAny;
    public static delegate* unmanaged<ref Name, ref GameplayTagContainer, NativeBool> MatchesAnyExact;
}