using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace UnrealSharpWeaver.Utilities;

public static class ParameterUtilities
{
    public static Instruction CreateLoadInstructionOutParam(this ParameterDefinition param, PropertyType paramTypeCode)
    {
        while (true)
        {
            switch (paramTypeCode)
            {
                case PropertyType.Enum:
                    var param1 = param;
                    param = null!;
                    paramTypeCode = param1.ParameterType.Resolve().GetEnumUnderlyingType().GetPrimitiveTypeCode();
                    continue;

                case PropertyType.Bool:
                case PropertyType.Int8:
                case PropertyType.Byte:
                    return Instruction.Create(OpCodes.Ldind_I1);

                case PropertyType.Int16:
                case PropertyType.UInt16:
                    return Instruction.Create(OpCodes.Ldind_I2);

                case PropertyType.Int:
                case PropertyType.UInt32:
                    return Instruction.Create(OpCodes.Ldind_I4);

                case PropertyType.Int64:
                case PropertyType.UInt64:
                    return Instruction.Create(OpCodes.Ldind_I8);

                case PropertyType.Float:
                    return Instruction.Create(OpCodes.Ldind_R4);

                case PropertyType.Double:
                    return Instruction.Create(OpCodes.Ldind_R8);

                case PropertyType.Struct:
                    return Instruction.Create(OpCodes.Ldobj, param.ParameterType.GetElementType());

                case PropertyType.LazyObject:
                case PropertyType.WeakObject:
                case PropertyType.SoftClass:
                case PropertyType.SoftObject:
                case PropertyType.Class:
                    return Instruction.Create(OpCodes.Ldobj, param.ParameterType.GetElementType());

                case PropertyType.Delegate:
                case PropertyType.MulticastInlineDelegate:
                case PropertyType.MulticastSparseDelegate:
                    // Delegate/multicast delegates in C# are implemented as classes, use Ldind_Ref
                    return Instruction.Create(OpCodes.Ldind_Ref);

                case PropertyType.InternalManagedFixedSizeArray:
                case PropertyType.InternalNativeFixedSizeArray:
                    throw new NotImplementedException(); // Fixed size arrays not supported as args

                case PropertyType.Array:
                case PropertyType.Set:
                case PropertyType.Map:
                    // Assumes this will be always be an object (IList, List, ISet, HashSet, IDictionary, Dictionary)
                    return Instruction.Create(OpCodes.Ldind_Ref);

                case PropertyType.Unknown:
                case PropertyType.Interface:
                case PropertyType.Object:
                case PropertyType.ObjectPtr:
                case PropertyType.String:
                case PropertyType.Name:
                case PropertyType.Text:
                case PropertyType.DefaultComponent:
                default:
                    return Instruction.Create(OpCodes.Ldind_Ref);
            }
        }
    }

    public static Instruction CreateSetInstructionOutParam(this ParameterDefinition param, PropertyType paramTypeCode)
    {
        while (true)
        {
            switch (paramTypeCode)
            {
                case PropertyType.Enum:
                    paramTypeCode = param.ParameterType.Resolve().GetEnumUnderlyingType().GetPrimitiveTypeCode();
                    continue;

                case PropertyType.Bool:
                case PropertyType.Int8:
                case PropertyType.Byte:
                    return Instruction.Create(OpCodes.Stind_I1);

                case PropertyType.Int16:
                case PropertyType.UInt16:
                    return Instruction.Create(OpCodes.Stind_I2);

                case PropertyType.Int:
                case PropertyType.UInt32:
                    return Instruction.Create(OpCodes.Stind_I4);

                case PropertyType.Int64:
                case PropertyType.UInt64:
                    return Instruction.Create(OpCodes.Stind_I8);

                case PropertyType.Float:
                    return Instruction.Create(OpCodes.Stind_R4);

                case PropertyType.Double:
                    return Instruction.Create(OpCodes.Stind_R8);

                case PropertyType.Struct:
                    return Instruction.Create(OpCodes.Stobj, param.ParameterType.GetElementType());

                case PropertyType.LazyObject:
                case PropertyType.WeakObject:
                case PropertyType.SoftClass:
                case PropertyType.SoftObject:
                case PropertyType.Class:
                case PropertyType.Name:
                case PropertyType.Text:
                    return Instruction.Create(OpCodes.Stobj, param.ParameterType.GetElementType());

                case PropertyType.Delegate:
                case PropertyType.MulticastSparseDelegate:
                case PropertyType.MulticastInlineDelegate:
                    // Delegate/multicast delegates in C# are implemented as classes, use Stind_Ref
                    return Instruction.Create(OpCodes.Stind_Ref);

                case PropertyType.InternalManagedFixedSizeArray:
                case PropertyType.InternalNativeFixedSizeArray:
                    throw new NotImplementedException(); // Fixed size arrays not supported as args

                case PropertyType.Array:
                case PropertyType.Set:
                case PropertyType.Map:
                    // Assumes this will be always be an object (IList, List, ISet, HashSet, IDictionary, Dictionary)
                    return Instruction.Create(OpCodes.Stind_Ref);

                case PropertyType.Unknown:
                case PropertyType.Interface:
                case PropertyType.Object:
                case PropertyType.ObjectPtr:
                case PropertyType.String:
                case PropertyType.DefaultComponent:
                default:
                    return Instruction.Create(OpCodes.Stind_Ref);
            }
        }
    }
}