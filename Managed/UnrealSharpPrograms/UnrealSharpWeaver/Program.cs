using System.Collections;
using System.Diagnostics;
using System.Text.Json;
using CommandLine;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Pdb;
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
    public static readonly string MulticastDelegatePropertyCallbacks = "FMulticastDelegatePropertyExporter";
    
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
        
        if (!LoadUserAssembly(weaverOptions))
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
    
    static bool LoadUserAssembly(WeaverOptions weaverOptions)
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
                ReadSymbols = true,
                SymbolReaderProvider = new PdbReaderProvider(),
            };

            AssemblyDefinition userAssembly = AssemblyDefinition.ReadAssembly(userAssemblyPath, readerParams);

            try
            {
                StartWeavingAssembly(userAssembly, weaverOutputPath);
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
    
    static void StartWeavingAssembly(AssemblyDefinition assembly, string assemblyOutputPath)
    {
        var assemblyMetaData = new ApiMetaData
        {
            AssemblyName = assembly.Name.Name,
        };
        
        WeaverHelper.ImportCommonTypes(assembly);

        StartProcessingAssembly(assembly, assemblyMetaData);

        string sourcePath = Path.GetDirectoryName(assembly.MainModule.FileName);
        CopyAssemblies(assemblyOutputPath, sourcePath);

        try
        {
            assembly.Write(assemblyOutputPath, new WriterParameters
            {
                WriteSymbols = true,
                SymbolWriterProvider = new PdbWriterProvider(),
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
            List<TypeDefinition> multicastDelegates = [];
            List<TypeDefinition> delegates = [];
            
            try
            {
                foreach (var module in userAssembly.Modules)
                {
                    foreach (var type in module.Types)
                    {
                        if (WeaverHelper.IsUnrealSharpClass(type))
                        {
                            classes.Add(type);
                        }
                        else if (WeaverHelper.IsUnrealSharpEnum(type))
                        {
                            enums.Add(type);
                        }
                        else if (WeaverHelper.IsUnrealSharpStruct(type))
                        {
                            structs.Add(type);
                        }
                        else if (WeaverHelper.IsUnrealSharpInterface(type))
                        {
                            interfaces.Add(type);
                        }
                        else if (type.BaseType != null && type.BaseType.Name.Contains("MulticastDelegate"))
                        {
                            multicastDelegates.Add(type);
                        }
                        else if (type.BaseType != null && type.BaseType.Name.Contains("Delegate"))
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
            
            UnrealDelegateProcessor.ProcessMulticastDelegates(multicastDelegates);
            UnrealDelegateProcessor.ProcessSingleDelegates(delegates);
            UnrealEnumProcessor.ProcessEnums(enums, metadata);
            UnrealInterfaceProcessor.ProcessInterfaces(interfaces, metadata);
            UnrealStructProcessor.ProcessStructs(structs, metadata, userAssembly);
            UnrealClassProcessor.ProcessClasses(classes, metadata);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error during assembly processing: {ex.Message}");
            throw;
        }
    }

    private static void CopyAssemblies(string destinationPath, string sourcePath)
    {
        var directoryName = Path.GetDirectoryName(destinationPath) ?? throw new InvalidOperationException("Assembly path does not have a valid directory.");

        if (!Directory.Exists(directoryName)) 
        {
            Directory.CreateDirectory(directoryName);
        }

        try
        {
            string[] dependencies = Directory.GetFiles(sourcePath, "*.*");
            foreach (var dependency in dependencies) 
            {
                var destPath = Path.Combine(directoryName, Path.GetFileName(dependency));
                if (!File.Exists(destPath) || new FileInfo(dependency).LastWriteTimeUtc > new FileInfo(destPath).LastWriteTimeUtc)
                {
                    File.Copy(dependency, destPath, true);
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to copy dependencies from {sourcePath}: {ex.Message}");
        }
    }
    
    static string StripQuotes (string s)
    {
        string strippedPath = s.Replace("\"", "");
        return strippedPath;
    }
}