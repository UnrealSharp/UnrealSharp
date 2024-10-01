using UnrealSharp.Attributes;
using UnrealSharp.GameplayTags;

namespace UnrealSharp.Interop;

[NativeCallbacks, InternalsVisible(true)]
internal static unsafe partial class FGameplayTagExporter
{
    public static delegate* unmanaged<ref FName, ref FName, NativeBool> MatchesTag;
    public static delegate* unmanaged<ref FName, ref FName, NativeBool> MatchesTagDepth;
    public static delegate* unmanaged<ref FName, ref GameplayTagContainer, NativeBool> MatchesAny;
    public static delegate* unmanaged<ref FName, ref GameplayTagContainer, NativeBool> MatchesAnyExact;
}