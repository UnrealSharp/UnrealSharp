using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace UnrealSharp.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class InternalsVisibleAttribute(bool IsPublic) : Attribute;
