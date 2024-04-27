using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.TypeProcessors;

public static class PropertyProcessor
{
    public static void ProcessClassMembers(
        ref List<Tuple<FieldDefinition, PropertyMetaData>> propertyOffsetsToInitialize,
        ref List<Tuple<FieldDefinition, PropertyMetaData>> propertyPointersToInitialize,
        TypeDefinition type,
        IEnumerable<PropertyMetaData> properties)
    {
        var removedBackingFields = new Dictionary<string, (PropertyMetaData, PropertyDefinition, FieldDefinition, FieldDefinition)>();

        foreach (var prop in properties)
        {
            FieldDefinition offsetField = AddOffsetField(type, prop, WeaverHelper.Int32TypeRef);
            FieldDefinition? nativePropertyField = AddNativePropertyField(type, prop, WeaverHelper.IntPtrType);
            
            propertyOffsetsToInitialize.Add(Tuple.Create(offsetField, prop));
            
            if (nativePropertyField != null)
            {
                prop.NativePropertyField = nativePropertyField;
                propertyPointersToInitialize.Add(Tuple.Create(nativePropertyField, prop));
            }
            
            if (prop.MemberRef.Resolve() is PropertyDefinition propertyRef)
            {
                prop.PropertyDataType.PrepareForRewrite(type, null, prop);
                prop.PropertyDataType.WriteGetter(type, propertyRef.GetMethod, offsetField, nativePropertyField);
                prop.PropertyDataType.WriteSetter(type, propertyRef.SetMethod, offsetField, nativePropertyField);

                string backingFieldName = RemovePropertyBackingField(type, prop);
                removedBackingFields.Add(backingFieldName, (prop, propertyRef, offsetField, nativePropertyField));
            }
            
            prop.PropertyOffsetField = offsetField;
            
        }

        RemoveBackingFieldReferences(type, removedBackingFields);
    }
    
    static void RemoveBackingFieldReferences(TypeDefinition type, Dictionary<string, (PropertyMetaData, PropertyDefinition, FieldDefinition, FieldDefinition)> strippedFields)
    {
        foreach (var ctor in type.GetConstructors().ToList())
        {
            if (!ctor.HasBody)
            {
                continue;
            }

            bool baseCallFound = false;
            var alteredInstructions = new List<Instruction>();
            var deferredInstructions = new List<Instruction>();

            foreach (var instr in ctor.Body.Instructions)
            {
                alteredInstructions.Add(instr);

                if (instr.Operand is MethodReference baseCtor && baseCtor.Name == ".ctor")
                {
                    baseCallFound = true;
                    alteredInstructions.AddRange(deferredInstructions);
                }

                if (instr.Operand is not FieldReference field)
                {
                    continue;
                }

                if (!strippedFields.TryGetValue(field.Name, out (PropertyMetaData meta, PropertyDefinition def, FieldDefinition offsetField,
                        FieldDefinition nativePropertyField) prop))
                {
                    continue;
                }

                // for now, we only handle simple stores
                if (instr.OpCode != OpCodes.Stfld)
                {
                    throw new UnableToFixPropertyBackingReferenceException(ctor, prop.def, instr.OpCode);
                }

                var m = prop.def.SetMethod;

                //if the property did not have a setter, add a private one for the ctor to use
                if (m == null)
                {
                    var voidRef = type.Module.ImportReference(typeof(void));
                    prop.def.SetMethod = m = new MethodDefinition($"set_{prop.def.Name}",
                        MethodAttributes.SpecialName | MethodAttributes.Private | MethodAttributes.HideBySig,
                        voidRef);
                    m.Parameters.Add(new ParameterDefinition(prop.def.PropertyType));
                    type.Methods.Add(m);
                    prop.meta.PropertyDataType.WriteSetter(type, prop.def.SetMethod, prop.offsetField, prop.nativePropertyField);
                }

                var newInstr = Instruction.Create((m.IsReuseSlot && m.IsVirtual) ? OpCodes.Callvirt : OpCodes.Call,
                    m);
                newInstr.Offset = instr.Offset;
                alteredInstructions[alteredInstructions.Count - 1] = newInstr;

                // now the hairy bit. initializers happen _before_ the base ctor call, so the NativeObject is not yet set, and they fail
                //we need to relocate these to after the base ctor call
                if (baseCallFound)
                {
                    // if they're after the base ctor call it's fine
                    continue;
                }

                //handle the simple pattern `ldarg0; ldconst*; call set_*`
                if (alteredInstructions[^3].OpCode != OpCodes.Ldarg_0)
                {
                    throw new UnsupportedPropertyInitializerException(prop.def); 
                }

                var ldconst = alteredInstructions[^2];
                
                if (!IsLdconst(ldconst))
                {
                    throw new UnsupportedPropertyInitializerException(prop.def);
                }
                
                CopyLastElements(alteredInstructions, deferredInstructions, 3);
            }

            //add back the instructions and fix up their offsets
            ctor.Body.Instructions.Clear();
            int offset = 0;
            foreach (var instr in alteredInstructions)
            {
                int oldOffset = instr.Offset;
                instr.Offset = offset;
                ctor.Body.Instructions.Add(instr);

                //fix up the sequence point offsets too
                if (ctor.DebugInformation == null || oldOffset == offset)
                {
                    continue;
                }
                
                //this only uses the offset so doesn't matter that we replaced the instruction
                var seqPoint = ctor.DebugInformation?.GetSequencePoint(instr);
                if (seqPoint == null)
                {
                    continue;
                }
                
                ctor.DebugInformation.SequencePoints.Remove(seqPoint);
                ctor.DebugInformation.SequencePoints.Add(
                    new SequencePoint(instr, seqPoint.Document)
                    {
                        StartLine = seqPoint.StartLine,
                        StartColumn = seqPoint.StartColumn,
                        EndLine = seqPoint.EndLine,
                        EndColumn = seqPoint.EndColumn
                    });
            }
        }
    }
        
    static string RemovePropertyBackingField(TypeDefinition type, PropertyMetaData prop)
    {
        string backingFieldName = $"<{prop.Name}>k__BackingField";

        for (var i = 0; i < type.Fields.Count; i++)
        {
            if (type.Fields[i].Name != backingFieldName)
            {
                continue;
            }
            
            type.Fields.RemoveAt(i);
            return backingFieldName;
        }
        
        throw new InvalidDataException($"Property '{prop.Name}' does not have a backing field");
    }

    public static FieldDefinition AddOffsetField(TypeDefinition type, PropertyMetaData prop, TypeReference int32TypeRef)
    {
        var field = new FieldDefinition(prop.Name + "_Offset",
            FieldAttributes.Static | FieldAttributes.Private, int32TypeRef);
        type.Fields.Add(field);
        return field;
    }

    public static FieldDefinition? AddNativePropertyField(TypeDefinition type, PropertyMetaData prop, TypeReference intPtrTypeRef)
    {
        if (!prop.PropertyDataType.NeedsNativePropertyField)
        {
            return null;
        }

        var field = new FieldDefinition(prop.Name + "_NativeProperty",
            FieldAttributes.InitOnly | FieldAttributes.Static | FieldAttributes.Private, intPtrTypeRef);
        type.Fields.Add(field);
        return field;
    }

    public static bool IsLdconst(Instruction ldconst)
    {
        return ldconst.OpCode.Op1 == 0xff && ldconst.OpCode.Op2 >= 0x15 && ldconst.OpCode.Op2 <= 0x23;
    }

    public static void CopyLastElements(List<Instruction> from, List<Instruction> to, int count)
    {
        int startIdx = from.Count - count;
        for (int i = startIdx; i < startIdx + count; i++)
        {
            to.Add(from[i]);
        }
        for (int i = startIdx + count -1; i >= startIdx; i--)
        {
            from.RemoveAt(i);
        }
    }
}