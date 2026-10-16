using System;
using System.Collections.Generic;
using EpicGames.UHT.Types;

namespace UnrealSharpManagedGlue.Utilities;

// UHT's resolved members are immutable during export. Keep their name indexes on
// the exporting thread, and release them when this type's export finishes.
internal sealed class NameCollisionScope : IDisposable
{
    [ThreadStatic] private static NameCollisionScope? _current;
    internal static NameCollisionScope? Current => _current;

    private readonly NameCollisionScope? _previous;
    internal readonly Dictionary<UhtType, string> ScriptNames = new();
    internal readonly Dictionary<UhtFunction, string> FunctionNames = new();
    internal readonly Dictionary<UhtProperty, string> PropertyNames = new();
    private readonly Dictionary<UhtType, MemberNames> _members = new();
    private readonly Dictionary<UhtClass, InterfaceNames> _interfaces = new();

    internal NameCollisionScope()
    {
        _previous = _current;
        _current = this;
    }

    public void Dispose()
    {
        _current = _previous;
        ScriptNames.Clear();
        FunctionNames.Clear();
        PropertyNames.Clear();
        _members.Clear();
        _interfaces.Clear();
    }

    private MemberNames GetMembers(UhtType owner)
    {
        if (!_members.TryGetValue(owner, out MemberNames? names))
        {
            names = new MemberNames(owner);
            _members.Add(owner, names);
        }
        return names;
    }

    internal bool HasChildConflict(UhtType type, string name)
    {
        MemberNames names = GetMembers(type.Outer!);
        return (names.Children.TryGetValue(name, out UhtType? child) && child != type)
            || (type is UhtProperty && type.Outer!.EngineType == UhtEngineType.Class
                && names.HasSynthesizedName(name));
    }

    internal UhtFunction? FindFunction(UhtClass owner, string name)
        => GetMembers(owner).Functions.GetValueOrDefault(name);

    internal UhtFunction? FindInterfaceFunction(UhtClass owner, string name, bool includeSynthesizedProperties)
    {
        if (!_interfaces.TryGetValue(owner, out InterfaceNames? names))
        {
            names = new InterfaceNames(owner);
            _interfaces.Add(owner, names);
        }
        return names.Find(name, includeSynthesizedProperties);
    }

    private sealed class MemberNames
    {
        // null records multiple owners: a HashSet alone cannot exclude the member itself.
        internal readonly Dictionary<string, UhtType?> Children = new(StringComparer.Ordinal);
        internal readonly Dictionary<string, UhtFunction> Functions = new(StringComparer.InvariantCultureIgnoreCase);
        private readonly UhtType _owner;
        private HashSet<string>? _synthesizedNames;

        internal MemberNames(UhtType owner)
        {
            _owner = owner;
            foreach (UhtType child in owner.Children)
            {
                string name;
                if (child is UhtProperty)
                {
                    name = NameMapper.ScriptifyName(child.GetScriptName(), ENameType.Property);
                }
                else if (child is UhtFunction function)
                {
                    name = NameMapper.ScriptifyName(child.GetScriptName(), ENameType.Function);
                    Functions.TryAdd(function.SourceName, function);
                }
                else
                {
                    continue;
                }
                if (!Children.TryAdd(name, child))
                {
                    Children[name] = null;
                }
            }
        }

        internal bool HasSynthesizedName(string name)
        {
            if (_synthesizedNames == null)
            {
                _synthesizedNames = new HashSet<string>(StringComparer.Ordinal);
                foreach (UhtType child in _owner.Children)
                {
                    if (child is UhtFunction function
                        && NameMapper.TryGetSynthesizedPropertyName(function, out string propertyName))
                    {
                        _synthesizedNames.Add(propertyName);
                    }
                }
            }
            return _synthesizedNames.Contains(name);
        }
    }

    private sealed class InterfaceNames
    {
        private readonly List<UhtFunction> _functions = new();
        private readonly Dictionary<string, int> _managed = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _native = new(StringComparer.InvariantCultureIgnoreCase);
        private Dictionary<string, int>? _synthesized;

        internal InterfaceNames(UhtClass owner)
        {
            foreach (UhtClass declaration in owner.GetInterfaces())
            {
                if (declaration.AlternateObject is not UhtClass interfaceClass)
                {
                    continue;
                }
                foreach (UhtFunction function in interfaceClass.Functions)
                {
                    int index = _functions.Count;
                    _functions.Add(function);
                    _managed.TryAdd(function.GetFunctionName(), index);
                    _native.TryAdd(function.SourceName, index);
                }
            }
        }

        internal UhtFunction? Find(string name, bool includeSynthesizedProperties)
        {
            int first = int.MaxValue;
            if (_managed.TryGetValue(name, out int managed)) first = managed;
            if (_native.TryGetValue(name, out int native)) first = Math.Min(first, native);
            if (includeSynthesizedProperties)
            {
                if (_synthesized == null)
                {
                    _synthesized = new Dictionary<string, int>(StringComparer.Ordinal);
                    for (int index = 0; index < _functions.Count; index++)
                    {
                        if (NameMapper.TryGetSynthesizedPropertyName(_functions[index], out string propertyName))
                        {
                            _synthesized.TryAdd(propertyName, index);
                        }
                    }
                }
                if (_synthesized.TryGetValue(name, out int synthesized)) first = Math.Min(first, synthesized);
            }
            return first == int.MaxValue ? null : _functions[first];
        }
    }
}
