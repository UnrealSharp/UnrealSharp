﻿using System.Text.Json;
using Mono.Cecil;
using Mono.Cecil.Pdb;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.TypeProcessors;

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
    
    public static WeaverOptions WeaverOptions { get; private set; }
    
    public static int Main(string[] args)
    {
        WeaverOptions = WeaverOptions.ParseArguments(args);

        if (!LoadBindingsAssembly())
        {
            return 1;
        }
        
        if (!StartProcessingUserAssembly())
        {
            return 2;
        }

        return 0;
    }

    private static bool LoadBindingsAssembly()
    {
        DefaultAssemblyResolver resolver = new DefaultAssemblyResolver();
        
        foreach (var assemblyPath in WeaverOptions.AssemblyPaths)
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

    private static bool StartProcessingUserAssembly()
    {
        string outputDirectory = StripQuotes(WeaverOptions.OutputDirectory);
        DirectoryInfo outputDirInfo = new DirectoryInfo(outputDirectory);
        
        if (!outputDirInfo.Exists)
        {
            outputDirInfo.Create();
        }

        foreach (var quotedAssemblyPath in WeaverOptions.AssemblyPaths)
        {
            var userAssemblyPath = Path.Combine(StripQuotes(quotedAssemblyPath), $"{WeaverOptions.ProjectName}.dll");

            if (!File.Exists(userAssemblyPath))
            {
                throw new FileNotFoundException($"Could not find UserAssembly at: {userAssemblyPath}");
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
                ErrorEmitter.Error(error.GetType().Name, error.File, error.Line, "Caught fatal error: " + error.Message);
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
        StartProcessingAssembly(assembly, ref assemblyMetaData);
        CopyAssemblyDependencies(assemblyOutputPath, Path.GetDirectoryName(assembly.MainModule.FileName)!);

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
            ErrorEmitter.Error("WeaverError", assembly.MainModule.FileName, 0, "Failed to write assembly: " + ex.Message);
            throw;
        }
        
        WriteAssemblyMetaDataFile(assemblyMetaData, assemblyOutputPath);
    }

    static void StartProcessingAssembly(AssemblyDefinition userAssembly, ref ApiMetaData metadata)
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
                        if (WeaverHelper.IsUClass(type))
                        {
                            classes.Add(type);
                        }
                        else if (WeaverHelper.IsUEnum(type))
                        {
                            enums.Add(type);
                        }
                        else if (WeaverHelper.IsUStruct(type))
                        {
                            structs.Add(type);
                        }
                        else if (WeaverHelper.IsUInterface(type))
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

    private static void CopyAssemblyDependencies(string destinationPath, string sourcePath)
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
            ErrorEmitter.Error("WeaverError", sourcePath, 0, "Failed to copy dependencies: " + ex.Message);
        }
    }

    private static string StripQuotes(string s)
    {
        string strippedPath = s.Replace("\"", "");
        return strippedPath;
    }
    
    private static void WriteAssemblyMetaDataFile(ApiMetaData metadata, string outputPath)
    {
        string metaDataContent = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        string metadataFilePath = Path.ChangeExtension(outputPath, "json");
        File.WriteAllText(metadataFilePath, metaDataContent);
    }
}