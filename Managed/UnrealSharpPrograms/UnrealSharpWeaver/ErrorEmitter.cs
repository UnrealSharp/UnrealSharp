using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UnrealSharpWeaver;

[Serializable]
class WeaverProcessError : Exception
{
    public string File { get; private set; } = string.Empty;
    public int Line { get; private set; }

    public WeaverProcessError(string message) : base(message) 
    {
        Line = -1;
    }

    public WeaverProcessError(string message, string file, int line) : base(message) 
    {
        File = file;
        Line = line;
    }

    public WeaverProcessError (string message, SequencePoint? point) : base(message)
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

    public WeaverProcessError(string message, Exception? innerException) : base(message,innerException) 
    {
        Line = -1;
    }

    public WeaverProcessError(string message, Exception? innerException, SequencePoint? point) : base(message, innerException)
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

    private static SequencePoint? ExtractFirstSequencePoint (MethodDefinition method)
    {
        return method?.DebugInformation?.SequencePoints.FirstOrDefault ();
    }

    public static SequencePoint? GetSequencePointFromMemberDefinition(IMemberDefinition member)
    {
        if (member is PropertyDefinition propertyDefinition)
        {
            SequencePoint? point = ExtractFirstSequencePoint(propertyDefinition.GetMethod);
            if (point != null)
            {
                return point;
            }
            
            point = ExtractFirstSequencePoint(propertyDefinition.SetMethod);
            if (point != null)
            {
                return point;
            }
            
            return GetSequencePointFromMemberDefinition(member.DeclaringType);
        }

        if (member is MethodDefinition definition)
        {
            SequencePoint? point = ExtractFirstSequencePoint(definition);
            if (point != null)
            {
                return point;
            }
            
            return GetSequencePointFromMemberDefinition(definition.DeclaringType);
        }
        
        if (member is TypeDefinition type)
        {
            foreach(MethodDefinition method in type.Methods)
            {
                SequencePoint? point = ExtractFirstSequencePoint(method);
                if (point != null)
                {
                    return point;
                }
            }
        }

        return null;
    }
}