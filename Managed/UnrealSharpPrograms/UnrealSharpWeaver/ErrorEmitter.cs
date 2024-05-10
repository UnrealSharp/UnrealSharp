using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UnrealSharpWeaver;

[Serializable]
class WeaverProcessError : Exception
{
    public string File { get; private set; }
    public int Line { get; private set; }

    public WeaverProcessError(string message)
        : base(message) 
    {
        Line = -1;
    }

    public WeaverProcessError(string message, string file, int line)
        : base(message) 
    {
        File = file;
        Line = line;
    }

    public WeaverProcessError (string message, SequencePoint point)
        : base(message)
    {
        if (point != null)
        {
            File = point.Document.Url.ToString();
            Line = point.StartLine;
        }
        else
        {
            Line = -1;
        }
    }

    public WeaverProcessError(string message, Exception innerException)
        : base(message,innerException) 
    {
        Line = -1;
    }

    public WeaverProcessError(string message, Exception innerException, SequencePoint point)
        : base(message, innerException)
    {
        if (point != null)
        {
            File = point.Document.Url;
            Line = point.StartLine;
        }
        else
        {
            Line = -1;
        }
    }


}

static class ErrorEmitter
{
    public static void Error (WeaverProcessError error)
    {
        Error(error.GetType().Name, error.File, error.Line, error.Message);

    }

    public static void Error(string code, string file, int line, string message)
    {
        if (!string.IsNullOrEmpty(file))
        {
            Console.Error.Write(file);
            if (line != -1)
            {
                Console.Error.Write("({0})",line);
            }

            Console.Error.Write(" : ");
        }
        else
        {
            Console.Error.Write("UnrealSharpWeaver: ");
        }

        Console.Error.WriteLine("error {0}: {1}",code,message);
    }

    public static Dictionary<TKey,TValue> ToDictionaryErrorEmit<TType,TKey, TValue> (this IEnumerable<TType> values, Func<TType,TKey> keyfunc, Func<TType,TValue> valuefunc, out bool hadError)
    {
        Dictionary<TKey, TValue> result = new Dictionary<TKey, TValue>();
        hadError = false;

        foreach (var item in values)
        {
            try
            {
                result.Add(keyfunc(item), valuefunc(item));
            }
            catch (WeaverProcessError ex)
            {
                hadError = true;
                Error(ex);
            }
        }

        return result;
    }

    private static SequencePoint ExtractFirstSequencePoint (MethodDefinition method)
    {
        return method?.DebugInformation?.SequencePoints.FirstOrDefault ();
    }

    public static SequencePoint GetSequencePointFromMemberDefinition(IMemberDefinition member)
    {
        if (member is PropertyDefinition)
        {
            PropertyDefinition prop = member as PropertyDefinition;
            SequencePoint point = ExtractFirstSequencePoint(prop.GetMethod);
            if (point != null)
            {
                return point;
            }
            point = ExtractFirstSequencePoint(prop.SetMethod);
            if (point != null)
            {
                return point;
            }
            return GetSequencePointFromMemberDefinition(member.DeclaringType);
        }
        else if (member is MethodDefinition)
        {
            MethodDefinition method = member as MethodDefinition;
            SequencePoint point = ExtractFirstSequencePoint(method);
            if (point != null)
            {
                return point;
            }
            return GetSequencePointFromMemberDefinition(member.DeclaringType);
        }
        else if (member is TypeDefinition)
        {
            TypeDefinition type = member as TypeDefinition;
            foreach(MethodDefinition method in type.Methods)
            {
                SequencePoint point = ExtractFirstSequencePoint(method);
                if (point != null)
                {
                    return point;
                }
            }
        }
        return null;
    }
}