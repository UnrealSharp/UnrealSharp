using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnrealSharpScriptGenerator;

public enum PropertyKind
{
    Unknown,
    Bool,
    Byte,
    Short,
    Int,
    Long,
    Float,
    Double,
    String,
    Enum
}