using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using UnrealSharp.SourceGenerators;
using Xunit;

namespace UnrealSharp.SourceGenerators.Tests;

public class NativeInteropSignatureAnalyzerTests
{
    private const string Stubs = """
        namespace UnrealSharp.Binds { [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)] public class NativeCallbacksAttribute : System.Attribute; }
        """;

    private static readonly ImmutableArray<MetadataReference> References = ((string) AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Select(path => (MetadataReference) MetadataReference.CreateFromFile(path))
        .ToImmutableArray();

    private static async Task<int> CountUs0015(string assemblyName, string source)
    {
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName,
            new[] { CSharpSyntaxTree.ParseText(Stubs), CSharpSyntaxTree.ParseText(source) },
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));

        ImmutableArray<Diagnostic> diagnostics = await compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new NativeInteropSignatureAnalyzer()))
            .GetAnalyzerDiagnosticsAsync();

        return diagnostics.Count(diagnostic => diagnostic.Id == "US0015");
    }

    [Theory]
    [InlineData("public unsafe class C { public delegate* unmanaged<bool, void> P; }")]
    [InlineData("public struct S { public char Value; } public unsafe class C { public delegate* unmanaged<S, void> P; }")]
    public async Task UnrealSharpAssembly_ReportsBoolAndChar(string source)
    {
        Assert.Equal(1, await CountUs0015("UnrealSharp.Core", source));
    }

    [Fact]
    public async Task GameAssembly_DoesNotReportUnmanagedFunctionPointerDelegate()
    {
        const string source = """
            using System.Runtime.InteropServices;
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void D(bool value);
            """;
        Assert.Equal(0, await CountUs0015("MyGame", source));
    }

    [Fact]
    public async Task GameAssembly_ReportsInsideNativeCallbacksClass()
    {
        const string source = """
            using System.Runtime.InteropServices;
            [UnrealSharp.Binds.NativeCallbacks]
            public class C { [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void D(bool value); }
            """;
        Assert.Equal(1, await CountUs0015("MyGame", source));
    }

    [Fact]
    public async Task MarshalAs_IsHonored()
    {
        const string source = """
            using System.Runtime.InteropServices;
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void D([MarshalAs(UnmanagedType.U1)] bool value);
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)] [return: MarshalAs(UnmanagedType.U1)] public delegate bool E();
            public struct S { [MarshalAs(UnmanagedType.U1)] public bool Value; }
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void F(S value);
            """;
        Assert.Equal(0, await CountUs0015("UnrealSharp.Core", source));
    }

    [Fact]
    public async Task LocalInitializedWithCast_ReportsOnce()
    {
        const string source = """
            public unsafe class C { public void M(void* x) { delegate* unmanaged<bool> p = (delegate* unmanaged<bool>) x; } }
            """;
        Assert.Equal(1, await CountUs0015("UnrealSharp.Core", source));
    }

    [Fact]
    public async Task FieldInitializedWithCast_ReportsOnce()
    {
        const string source = """
            public unsafe class C { public static void* X; public delegate* unmanaged<bool> P = (delegate* unmanaged<bool>) X; }
            """;
        Assert.Equal(1, await CountUs0015("UnrealSharp.Core", source));
    }

    [Fact]
    public async Task ManagedFunctionPointer_IsNotReported()
    {
        const string source = "public unsafe class C { public delegate*<bool, void> P; }";
        Assert.Equal(0, await CountUs0015("UnrealSharp.Core", source));
    }
}
