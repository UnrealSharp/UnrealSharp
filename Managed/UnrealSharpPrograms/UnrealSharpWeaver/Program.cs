using System.Collections;
using System.Diagnostics;
using System.Text.Json;
using CommandLine;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.Rewriters;

namespace UnrealSharpWeaver;

public static class Program
{
    public static readonly string UnrealSharpNamespace = "UnrealSharp";
    public static readonly string InteropNameSpace = UnrealSharpNamespace + ".Interop";
    public static readonly string AttributeNamespace = UnrealSharpNamespace + ".Attributes";
    public static readonly string UnrealSharpObjectName = "UnrealSharpObject";
    public static readonly string FPropertyCallbacks = "FPropertyExporter";
    public static readonly string UClassCallbacks = "UClassExporter";
    public static readonly string CoreUObjectCallbacks = "UCoreUObjectExporter";
    public static readonly string FBoolPropertyCallbacks = "FBoolPropertyExporter";
    public static readonly string FStringCallbacks = "FStringExporter";
    public static readonly string UObjectCallbacks = "UObjectExporter";
    public static readonly string FArrayPropertyCallbacks = "FArrayPropertyExporter";
    public static readonly string UScriptStructCallbacks = "UScriptStructExporter";
    public static readonly string UFunctionCallbacks = "UFunctionExporter";
    public static readonly string MarshallerSuffix = "Marshaller";
    
    public static int Main(string[] args)
    {
        WeaverOptions? weaverOptions = WeaverOptions.ParseArguments(args);

        if (weaverOptions == null)
        {
            Console.Error.WriteLine("Invalid arguments.");
            return 1;
        }

        DefaultAssemblyResolver resolver = new DefaultAssemblyResolver();

        if (!LoadBindingsAssembly(weaverOptions, resolver))
        {
            return 1;
        }
        
        if (!LoadUserAssembly(weaverOptions, resolver))
        {
            return 2;
        }

        return 0;
    }
    
    static bool LoadBindingsAssembly(WeaverOptions weaverOptions, DefaultAssemblyResolver resolver)
    {
        foreach (var assemblyPath in weaverOptions.AssemblyPaths)
        {
            if (Directory.Exists(assemblyPath))
            {
                resolver.AddSearchDirectory(assemblyPath);
            }
        }

        try
        {
            var unrealSharpLibraryAssembly = resolver.Resolve(new AssemblyNameReference(UnrealSharpNamespace, new Version(0, 0, 0, 0)));
            WeaverHelper.Initialize(unrealSharpLibraryAssembly);
            return true;
        }
        catch
        {
            Console.Error.WriteLine("Could not resolve the UnrealSharp library assembly.");
        }
        
        return false;
    }
    
    static bool LoadUserAssembly(WeaverOptions weaverOptions, DefaultAssemblyResolver resolver)
    {
        string outputDirectory = StripQuotes(weaverOptions.OutputDirectory);
        DirectoryInfo outputDirInfo = new DirectoryInfo(outputDirectory);
        
        if (!outputDirInfo.Exists)
        {
            outputDirInfo.Create();
        }

        foreach (var quotedAssemblyPath in weaverOptions.AssemblyPaths)
        {
            var userAssemblyPath = Path.Combine(StripQuotes(quotedAssemblyPath), $"{weaverOptions.ProjectName}.dll");

            if (!File.Exists(userAssemblyPath))
            {
                Console.Error.WriteLine($"Could not find UserAssembly at: {userAssemblyPath}");
            }

            var weaverOutputPath = Path.Combine(outputDirectory, Path.GetFileName(userAssemblyPath));
            
            var readerParams = new ReaderParameters
            {
                AssemblyResolver = resolver,
                ReadSymbols = true,
                SymbolReaderProvider = new PortablePdbReaderProvider(),
                ReadingMode = ReadingMode.Deferred
            };

            AssemblyDefinition userAssembly = AssemblyDefinition.ReadAssembly(userAssemblyPath, readerParams);

            try
            {
                StartWeavingAssembly(userAssembly, userAssemblyPath, weaverOutputPath, resolver);
                return true;
            }
            catch (WeaverProcessError error)
            {
                ErrorEmitter.Error(error.GetType().Name, error.File, error.Line, "UNCAUGHT NON-FATAL ERROR: " + error.Message);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception processing {userAssemblyPath}: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
            }
        }

        return false;
    }
    
    static void StartWeavingAssembly(AssemblyDefinition assembly, string assemblyPath, string assemblyOutputPath, BaseAssemblyResolver resolver)
    {
        var assemblyMetaData = new ApiMetaData
        {
            AssemblyName = assembly.Name.Name,
        };
        
        WeaverHelper.ImportCommonTypes(assembly);

        StartProcessingAssembly(assembly, assemblyMetaData);
        CopyDependencies(assemblyOutputPath, resolver.GetSearchDirectories());

        try
        {
            assembly.Write(assemblyOutputPath, new WriterParameters
            {
                WriteSymbols = true,
                SymbolWriterProvider = new PortablePdbWriterProvider(),
            });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error writing assembly to disk: {ex.Message}");
            throw;
        }

        var metadataJsonString = JsonSerializer.Serialize(assemblyMetaData, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        string metadataFilePath = Path.ChangeExtension(assemblyOutputPath, "json");
        File.WriteAllText(metadataFilePath, metadataJsonString);
    }

    static void StartProcessingAssembly(AssemblyDefinition userAssembly, ApiMetaData metadata)
    {
        try
        {
            List<TypeDefinition> classes = [];
            List<TypeDefinition> structs = [];
            List<TypeDefinition> enums = [];
            List<TypeDefinition> interfaces = [];
            List<TypeDefinition> delegates = [];
            
            try
            {
                foreach (var module in userAssembly.Modules)
                {
                    foreach (var type in module.Types)
                    {
                        if (type.IsClass && type.BaseType != null && WeaverHelper.IsUnrealSharpClass(type))
                        {
                            classes.Add(type);
                        }
                        else if (type.IsEnum && WeaverHelper.IsUnrealSharpEnum(type))
                        {
                            enums.Add(type);
                        }
                        else if (type.IsValueType && WeaverHelper.IsUnrealSharpStruct(type))
                        {
                            structs.Add(type);
                        }
                        else if (type.IsInterface && InterfaceMetaData.IsBlueprintInterface(type))
                        {
                            interfaces.Add(type);
                        }
                        else if (type.BaseType is { Name: "EventDispatcher" })
                        {
                            delegates.Add(type);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error enumerating types: {ex.Message}");
                throw;
            }
            
            UnrealEnumProcessor.ProcessEnums(enums, metadata);
            UnrealInterfaceProcessor.ProcessInterfaces(interfaces, metadata);
            UnrealStructProcessor.ProcessStructs(structs, metadata, userAssembly);
            UnrealClassProcessor.ProcessClasses(classes, metadata);
            UnrealDelegateProcessor.ProcessDelegateExtensions(delegates);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error during assembly processing: {ex.Message}");
            throw;
        }
    }

    private static void CopyDependencies(string assemblyPath, string[] knownPaths)
    {
        string assemblyDirectory = Path.GetDirectoryName(assemblyPath) ?? throw new InvalidOperationException("Assembly path does not have a valid directory.");

        if (!Directory.Exists(assemblyDirectory)) 
        {
            Directory.CreateDirectory(assemblyDirectory);
        }

        foreach (var path in knownPaths) 
        {
            if (!Directory.Exists(path))
            {
                continue;
            }

            try
            {
                string[] dlls = Directory.GetFiles(path, "*.dll");
                foreach (var dll in dlls) 
                {
                    var destPath = Path.Combine(assemblyDirectory, Path.GetFileName(dll));
                    if (!File.Exists(destPath) || new FileInfo(dll).LastWriteTimeUtc > new FileInfo(destPath).LastWriteTimeUtc)
                    {
                        File.Copy(dll, destPath, true);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to copy dependencies from {path}: {ex.Message}");
            }
        }
    }
    
    static string StripQuotes (string s)
    {
        string strippedPath = s.Replace("\"", "");
        return strippedPath;
    }
}