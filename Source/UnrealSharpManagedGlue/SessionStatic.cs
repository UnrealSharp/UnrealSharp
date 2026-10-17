using System;
using System.Runtime.CompilerServices;
using System.Threading;
using EpicGames.UHT.Utils;
using UnrealSharpManagedGlue.PropertyTranslators;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue;

// UHT can run multiple exporters in one process. AsyncLocal also carries the
// current session into the export tasks created by IUhtExportFactory.
public sealed class SessionStatic : IDisposable
{
    private static readonly AsyncLocal<SessionStatic?> Current = new();
    private static readonly ConditionalWeakTable<UhtSession, object> InitializedSessions = new();

    private readonly GeneratorStatics _generator = new();
    private readonly ModuleUtilities _modules = new();
    private readonly FileExporter _files = new();
    private readonly TaskManager _tasks = new();
    private readonly PackageHeadersTracker _headers = new();
    private readonly PropertyTranslatorManager _translators = new();
    private readonly InclusionLists _inclusions = new();
    private readonly ModuleFactory _moduleFactory = new();
    private readonly UnrealSharpSettingsUtilities _settings = new();

    private static SessionStatic Instance => Current.Value ?? throw new InvalidOperationException("No active UnrealSharp UHT session.");

    public static GeneratorStatics Generator => Instance._generator;
    public static ModuleUtilities Modules => Instance._modules;
    public static FileExporter Files => Instance._files;
    public static TaskManager Tasks => Instance._tasks;
    public static PackageHeadersTracker Headers => Instance._headers;
    public static PropertyTranslatorManager Translators => Instance._translators;
    public static InclusionLists Inclusions => Instance._inclusions;
    public static ModuleFactory ModuleFactory => Instance._moduleFactory;
    public static UnrealSharpSettingsUtilities Settings => Instance._settings;

    public SessionStatic(IUhtExportFactory factory)
    {
        lock (InitializedSessions)
        {
            if (Current.Value != null || InitializedSessions.TryGetValue(factory.Session, out _))
            {
                factory.Session.LogError("UnrealSharp exporter was initialized more than once for this UHT session.");
                throw new InvalidOperationException("UnrealSharp UHT session already initialized.");
            }

            InitializedSessions.Add(factory.Session, new object());
        }

        Current.Value = this;
        try
        {
            _generator.Initialize(factory);
            _translators.Initialize();
            _moduleFactory.Initialize();
        }
        catch
        {
            Current.Value = null;
            throw;
        }
    }

    public void Dispose()
    {
        try
        {
            // Drain tasks even if generation threw before its normal wait.
            _tasks.WaitForTasks();
        }
        finally
        {
            Current.Value = null;
        }
    }
}
