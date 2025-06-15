using Mono.Cecil;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver.MetaData;

public class DelegateMetaData : TypeReferenceMetadata
{
    public FunctionMetaData Signature { get; set; }
    
    public DelegateMetaData(FunctionMetaData signature, TypeReference member, string attributeName = "", EFunctionFlags functionFlags = EFunctionFlags.None) : base(member, attributeName)
    {
        Name = DelegateUtilities.GetUnrealDelegateName(member);
        
        Signature = signature;
        Signature.FunctionFlags |= functionFlags;
    }
}