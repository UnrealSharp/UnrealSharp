using Mono.Cecil;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.NativeTypes;
class NativeDataCoreStructType : NativeDataBlittableStructTypeBase
{ 
    public TypeReferenceMetadata InnerType { get; set; }

    public NativeDataCoreStructType(TypeReference structType, int arrayDim) : base(structType, arrayDim)
    {
        var innerPropertyName = structType.Name switch
        {
            "Quaternion" => "Quat",
            "Vector2" => "Vector2D",
            "Vector3" => "Vector",
            "Matrix4x4" => "Matrix",
            _ => structType.Name
        };

        InnerType = new TypeReferenceMetadata(structType.Resolve())
        {
            Name = innerPropertyName
        };
    }
}