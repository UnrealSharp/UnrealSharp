using System.Reflection;
using CommandLine;

namespace UnrealSharpBuildTool;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class ActionAttribute : Attribute
{
    public string Name { get; set; }
    public string Description { get; set; }

    public ActionAttribute(string name, string description)
    {
        Name = name;
        Description = description;
    }
}

public static class ActionManager
{
    public static IList<MethodInfo> GetActions()
    {
        IList<MethodInfo> actions = new List<MethodInfo>();
        Assembly assembly = Assembly.GetExecutingAssembly();

        foreach (Type type in assembly.GetTypes())
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                ActionAttribute? actionAttribute = method.GetCustomAttribute<ActionAttribute>();
                if (actionAttribute != null)
                {
                    actions.Add(method);
                }
            }
        }
        return actions;
    }

    public static void RunAction(string action, IEnumerable<string> args)
    {
        IList<MethodInfo> actions = GetActions();
        
        MethodInfo? methodToRun = null;
        foreach (MethodInfo actionMethod in actions)
        {
            ActionAttribute? actionAttribute = actionMethod.GetCustomAttribute<ActionAttribute>();
            if (actionAttribute == null || !actionAttribute.Name.Equals(action, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            
            methodToRun = actionMethod;
            break;
        }

        if (methodToRun == null)
        {
            Console.WriteLine($"Action '{action}' not found.");
            return;
        }

        ParameterInfo[] parameters = methodToRun.GetParameters();
        object?[] parameterValues = new object[parameters.Length];
        
        for (int i = 0; i < parameters.Length; i++)
        {
            ParameterInfo parameter = parameters[i];
            object? parameterValue = Activator.CreateInstance(parameter.ParameterType);
            
            if (parameterValue == null)
            {
                throw new InvalidOperationException($"Failed to create an instance of type '{parameter.ParameterType.Name}' for action parameter {methodToRun.Name}.{parameter.Name}.");
            }
            
            parameterValues[i] = parameterValue;
            IterateTypeMembers(args, parameter.ParameterType, parameterValue);
        }
        
        methodToRun.Invoke(null, parameterValues);
    }

    private static void IterateTypeMembers(IEnumerable<string> args, Type parameterType, object instance)
    {
        foreach (PropertyInfo member in parameterType.GetProperties())
        {
            Type memberType = member.PropertyType;
                    
            OptionAttribute? optionAttribute = member.GetCustomAttribute<OptionAttribute>();
            if (optionAttribute == null)
            {
                continue;
            }
                    
            if (memberType.IsValueType && !memberType.IsPrimitive)
            {
                object? value = Activator.CreateInstance(memberType);
                
                if (value == null)
                {
                    throw new InvalidOperationException($"Failed to create an instance of type '{memberType.Name}' for action option {member.DeclaringType!.Name}.{member.Name}.");
                }
                
                IterateTypeMembers(args, member.PropertyType, value);
                member.SetValue(instance, value);
            }
            else
            {
                string? optionValue = args.FirstOrDefault(arg => arg.StartsWith($"{optionAttribute.LongName}=", StringComparison.OrdinalIgnoreCase) || arg.StartsWith($"{optionAttribute.ShortName}=", StringComparison.OrdinalIgnoreCase));
                
                if (optionValue == null && optionAttribute.Required)
                {
                    throw new ArgumentException($"Required option '{optionAttribute.LongName}' is missing for action option {member.DeclaringType!.Name}.{member.Name}.");
                }
                        
                if (optionValue == null)
                {
                    continue;
                }
                
                string value = optionValue.Split('=')[1];
                object convertedValue = Convert.ChangeType(value, member.PropertyType);
                member.SetValue(instance, convertedValue);
            }
        }
    }
    
    public static void PrintActions()
    {
        Console.WriteLine("Available Actions:");
        Console.WriteLine("========================================");
        Console.WriteLine();

        IList<MethodInfo> actions = GetActions();
        foreach (MethodInfo action in actions)
        {
            ActionAttribute? actionAttribute = action.GetCustomAttribute<ActionAttribute>();
            if (actionAttribute == null)
            {
                continue;
            }

            Console.WriteLine($"Action: {actionAttribute.Name}");
            Console.WriteLine($"Description: {actionAttribute.Description}");
            
            if (action.GetParameters().Length > 0)
            {
                Console.WriteLine("Options:");
                foreach (ParameterInfo parameter in action.GetParameters())
                {
                    RecursivePrintOptions(parameter.ParameterType, 2);
                }
            }
            
            Console.WriteLine();
        }

        void RecursivePrintOptions(Type type, int indentLevel)
        {
            if (type == typeof(string) || type.IsPrimitive || type.IsEnum)
            {
                return;
            }
            
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                OptionAttribute? option = property.GetCustomAttribute<OptionAttribute>();
                if (option == null)
                {
                    continue;
                }
                
                string shortName = string.IsNullOrEmpty(option.ShortName) ? string.Empty : $" ({option.ShortName})";
                Console.WriteLine($"{new string(' ', indentLevel * 2)}--{option.LongName}{shortName}: {option.HelpText} (Required: {option.Required})");
                
                if ((property.PropertyType.IsClass || property.PropertyType.IsValueType) &&
                    property.PropertyType != typeof(string) &&
                    !property.PropertyType.IsPrimitive &&
                    !property.PropertyType.IsEnum)
                {
                    RecursivePrintOptions(property.PropertyType, indentLevel + 1);
                }
            }
        }
        
        Console.WriteLine("========================================");
    }
}