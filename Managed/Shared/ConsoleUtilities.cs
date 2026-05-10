using System;

namespace UnrealSharp.Shared;

public static class ConsoleUtilities
{
    public static void Log(string message)
    {
        Console.WriteLine("[UnrealSharp] " + message);
    }
}