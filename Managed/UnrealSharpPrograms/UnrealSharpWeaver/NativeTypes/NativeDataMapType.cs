using Mono.Cecil;
using Mono.Cecil.Cil;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.NativeTypes;

public class NativeDataMapType : NativeDataContainerType
{
    public PropertyMetaData ValueProperty { get; set; }
    
    public NativeDataMapType(TypeReference typeRef, int arrayDim, TypeReference key, TypeReference value) : base(typeRef, arrayDim, PropertyType.Map, key)
    {
        ValueProperty = PropertyMetaData.FromTypeReference(value, "Value");
        IsNetworkSupported = false;
    }

    public override string GetContainerMarshallerName()
    {
        return "MapMarshaller`2";
    }

    public override string GetCopyContainerMarshallerName()
    {
        return "MapCopyMarshaller`2";
    }

    public override void EmitDynamicArrayMarshallerDelegates(ILProcessor processor, TypeDefinition type)
    {
        base.EmitDynamicArrayMarshallerDelegates(processor, type);
        ValueProperty.PropertyDataType.EmitDynamicArrayMarshallerDelegates(processor, type);
    }

    public override void PrepareForRewrite(TypeDefinition typeDefinition, FunctionMetaData? functionMetadata, PropertyMetaData propertyMetadata)
    {
        base.PrepareForRewrite(typeDefinition, functionMetadata, propertyMetadata);
        ValueProperty.PropertyDataType.PrepareForRewrite(typeDefinition, functionMetadata, propertyMetadata);
    }

    public override void InitializeMarshallerParameters()
    {
        ContainerMarshallerTypeParameters =
        [
            WeaverHelper.ImportType(InnerProperty.PropertyDataType.CSharpType),
            WeaverHelper.ImportType(ValueProperty.PropertyDataType.CSharpType)
        ];
    }

    public override string GetContainerWrapperType()
    {
        return "System.Collections.Generic.IDictionary`2";
    }
}