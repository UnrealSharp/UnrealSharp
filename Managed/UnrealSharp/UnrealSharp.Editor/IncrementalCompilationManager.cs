using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using UnrealSharp.Editor.Utilities;
using UnrealSharp.Shared;
using UnrealSharp.UnrealSharpUtilities;

namespace UnrealSharp.Editor;

public static class IncrementalCompilationManager
{
    private readonly record struct CompiledProject(Project Project, GenState State, Compilation Compilation, byte[] Image);

    private static bool NeedsProjectStateRefresh(GenState? state)
    {
        return state == null
               || state.Driver == null
               || state.InitialCompilation == null
               || state.TreesByPath == null;
    }

    private static bool IsSkippablePath(string path)
    {
        string normalized = path.Replace('\\', '/');
        return normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase);
    }

    public static void MarkSourceFileDirty(string projectName, string filepath)
    {
        Project? foundProject = SolutionManager.GetProjectByName(projectName);

        if (foundProject is null)
        {
            throw new Exception($"Project '{projectName}' not found in solution.");
        }

        GenState? state = SolutionManager.GetProjectState(foundProject.Id);
        if (state == null)
        {
            throw new Exception($"Project '{projectName}' not initialized for incremental generation.");
        }

        string fullPath = Path.GetFullPath(filepath);
        if (IsSkippablePath(fullPath))
        {
            return;
        }

        state.DirtyPaths.Add(fullPath);
    }

    private static void RemoveSyntaxTree(GenState state, string fullPath)
    {
        if (!state.TreesByPath!.TryGetValue(fullPath, out SyntaxTree? existingTree))
        {
            return;
        }

        state.InitialCompilation = state.InitialCompilation!.RemoveSyntaxTrees(existingTree);
        state.TreesByPath.Remove(fullPath);
    }

    private static void ApplySourceFile(Project project, GenState state, string fullPath)
    {
        SourceText newText = SourceText.From(File.ReadAllText(fullPath), Encoding.UTF8);

        CSharpParseOptions parseOptions = (CSharpParseOptions)project.ParseOptions!;
        SyntaxTree newTree = CSharpSyntaxTree.ParseText(newText, parseOptions, path: fullPath);

        string errors = FormatErrors(newTree.GetDiagnostics());
        if (errors.Length > 0)
        {
            throw new Exception(errors);
        }

        if (state.TreesByPath!.TryGetValue(fullPath, out SyntaxTree? existingTree))
        {
            state.InitialCompilation = state.InitialCompilation!.ReplaceSyntaxTree(existingTree, newTree);
        }
        else
        {
            state.InitialCompilation = state.InitialCompilation!.AddSyntaxTrees(newTree);
        }

        state.TreesByPath[fullPath] = newTree;
    }

    private static void ApplyDirtyPathsFromDisk(Project project, GenState state)
    {
        StringBuilder errors = new StringBuilder();

        foreach (string fullPath in state.DirtyPaths.ToList())
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                if (File.Exists(fullPath))
                {
                    ApplySourceFile(project, state, fullPath);
                }
                else
                {
                    RemoveSyntaxTree(state, fullPath);
                }

                state.DirtyPaths.Remove(fullPath);
                state.ChangedSinceLoad.Add(fullPath);
                string fileName = Path.GetFileName(fullPath);
                LogUnrealSharpEditor.Log($"Picked up changes to {fileName} in {stopwatch.Elapsed.TotalMilliseconds:F2} ms.");
            }
            catch (Exception exception)
            {
                errors.AppendLine(exception.Message);
            }
        }

        if (errors.Length > 0)
        {
            throw new Exception(errors.ToString());
        }
    }

    public static bool RecompileDirtyProjects(List<string> modifiedAssemblyNames, bool allowFullReload)
    {
        List<Project> projects = ProjectUtilities.GetProjectsFromNames(modifiedAssemblyNames, SolutionManager.CurrentProjects);

        if (projects.Count != modifiedAssemblyNames.Count)
        {
            IEnumerable<string> missingAssemblyNames = modifiedAssemblyNames.Except(
                projects.Select(project => project.Name), StringComparer.Ordinal);
            throw new InvalidOperationException("Modified assemblies could not be resolved to Roslyn projects: "
                                                + string.Join(", ", missingAssemblyNames));
        }

        List<CompiledProject> compiledProjects = new List<CompiledProject>(projects.Count);

        for (int i = projects.Count - 1; i >= 0; i--)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Project project = projects[i];

            LogUnrealSharpEditor.Log($"Starting source generation for project '{project.Name}'.");

            GenState? state = SolutionManager.GetProjectState(project.Id);

            if (NeedsProjectStateRefresh(state))
            {
                LogUnrealSharpEditor.LogWarning(
                    $"Project '{project.Name}' incremental state is invalid. Rebuilding project state.");
                SolutionManager.ProcessProject(project).GetAwaiter().GetResult();
                state = SolutionManager.GetProjectState(project.Id);
            }

            if (state is null)
            {
                throw new Exception($"Project '{project.Name}' not initialized for incremental generation.");
            }

            ApplyDirtyPathsFromDisk(project, state);

            state.Driver = state.Driver!.RunGeneratorsAndUpdateCompilation(
                state.InitialCompilation!,
                out Compilation updatedCompilation,
                out ImmutableArray<Diagnostic> generatorDiagnostics);

            string generatorErrors = FormatErrors(generatorDiagnostics);
            if (generatorErrors.Length > 0)
            {
                throw new Exception("Source generator failed:\n" + generatorErrors);
            }

            UpdateDependentProjectsWithNewCompilation(updatedCompilation, project);

            stopwatch.Stop();
            LogUnrealSharpEditor.Log($"Project '{project.Name}' generated source in {stopwatch.Elapsed.TotalSeconds:F2} seconds.");

            byte[] image = EmitResultsToDisk(project, updatedCompilation);
            compiledProjects.Add(new CompiledProject(project, state, updatedCompilation, image));
        }

        List<string> assemblies = new List<string>(SolutionManager.UnrealSharpWorkspace.CurrentSolution.Projects.Count());

        foreach (Project project in SolutionManager.UnrealSharpWorkspace.CurrentSolution.Projects)
        {
            assemblies.Add(GetAssemblyOutputPath(project));
        }

        string outputDir = Path.GetDirectoryName(assemblies[0])!;

        LoadOrderOptions loadOrderOptions = new LoadOrderOptions() { Collectible = true, Priority = 0 };
        AssemblyUtilities.EmitLoadOrder(assemblies, outputDir, loadOrderOptions, "UserCode");

        bool patchedAll = true;
        foreach ((Project project, GenState state, Compilation compilation, _) in compiledProjects)
        {
            if (LoadedAssemblyPatcher.TryPatchLoadedAssembly(project, state, compilation))
            {
                continue;
            }

            patchedAll = false;
            break;
        }

        if (patchedAll)
        {
            return true;
        }

        if (!allowFullReload)
        {
            return false;
        }

        foreach (CompiledProject compiledProject in compiledProjects)
        {
            PrepareFullReload(compiledProject);
        }

        return false;
    }

    private static void PrepareFullReload(CompiledProject compiledProject)
    {
        GenState state = compiledProject.State;

        foreach (string path in state.ChangedSinceLoad)
        {
            if (!state.TreesByPath!.TryGetValue(path, out SyntaxTree? newTree))
            {
                continue;
            }

            state.LoadedTreesByPath.TryGetValue(path, out SyntaxTree? loadedTree);
            SyntaxUtilities.LookForChangesInUnrealTypes(newTree, loadedTree, compiledProject.Project);
        }

        ModuleMetadata metadata = ModuleMetadata.CreateFromImage(compiledProject.Image);
        Compilation compilation = compiledProject.Compilation;
        const bool HasPortablePdb = true;
        EmitBaseline baseline = EmitBaseline.CreateInitialBaseline(compilation, metadata, _ => default, _ => default, HasPortablePdb);
        state.MarkLoaded(compilation, baseline);
    }

    private static void UpdateDependentProjectsWithNewCompilation(Compilation newCompilation, Project producedProject)
    {
        IEnumerable<Project> dependentProjects = producedProject.GetDependentProjects(SolutionManager.CurrentProjects);

        foreach (Project dependentProject in dependentProjects)
        {
            GenState? projectState = SolutionManager.GetProjectState(dependentProject.Id);

            if (projectState is null)
            {
                throw new Exception($"Project '{dependentProject.Name}' not initialized for incremental generation.");
            }

            if (projectState.InitialCompilation is null)
            {
                throw new Exception($"Project '{dependentProject.Name}' has no initial compilation.");
            }

            Compilation depCompilation = projectState.InitialCompilation;

            MetadataReference? oldCompilationReference = null;
            foreach (MetadataReference reference in depCompilation.References)
            {
                if (reference is not CompilationReference compilationReference)
                {
                    continue;
                }

                if (compilationReference.Compilation.AssemblyName != newCompilation.AssemblyName)
                {
                    continue;
                }

                oldCompilationReference = reference;
                break;
            }

            CompilationReference newCompilationReference = newCompilation.ToMetadataReference();

            if (oldCompilationReference != null)
            {
                depCompilation = depCompilation.ReplaceReference(oldCompilationReference, newCompilationReference);
            }
            else
            {
                depCompilation = depCompilation.AddReferences(newCompilationReference);
            }

            projectState.InitialCompilation = depCompilation;
        }
    }

    private static string GetOutputPath(Project project, string extension)
    {
        string userAssemblyDir = UCSPathsBlueprintFunctionLibrary.UserAssemblyDirectory;

        if (!Directory.Exists(userAssemblyDir))
        {
            Directory.CreateDirectory(userAssemblyDir);
        }

        return Path.Combine(userAssemblyDir, project.AssemblyName + extension);
    }

    private static string GetAssemblyOutputPath(Project project)
    {
        string extension = project.OutputFilePath != null ? Path.GetExtension(project.OutputFilePath) : ".dll";
        return GetOutputPath(project, extension);
    }

    private static byte[] EmitResultsToDisk(Project project, Compilation updatedCompilation)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        using MemoryStream assemblyStream = new MemoryStream();
        using MemoryStream symbolsStream = new MemoryStream();
        EmitOptions emitOptions = new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb);
        EmitResult emitResult = updatedCompilation.Emit(assemblyStream, symbolsStream, options: emitOptions);

        if (!emitResult.Success)
        {
            throw new InvalidOperationException(FormatErrors(emitResult.Diagnostics));
        }

        byte[] image = assemblyStream.ToArray();
        WriteFileAtomically(GetAssemblyOutputPath(project), image);
        WriteFileAtomically(GetOutputPath(project, ".pdb"), symbolsStream.ToArray());

        stopwatch.Stop();
        LogUnrealSharpEditor.Log($"Project '{project.Name}' produced an assembly in {stopwatch.Elapsed.TotalSeconds:F2} seconds.");
        return image;
    }

    private static void WriteFileAtomically(string path, byte[] bytes)
    {
        string tempPath = path + ".tmp";
        File.WriteAllBytes(tempPath, bytes);
        File.Move(tempPath, path, true);
    }

    public static string FormatErrors(IEnumerable<Diagnostic> diagnostics)
    {
        return string.Join(Environment.NewLine, diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
    }
}
