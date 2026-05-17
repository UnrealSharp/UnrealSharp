using System;

namespace UnrealSharpManagedGlue.Utilities;

public static class ConsoleUtilities
{
    public static void Log(string message)
    {
        Console.WriteLine("[UnrealSharp] " + message);
    }
}